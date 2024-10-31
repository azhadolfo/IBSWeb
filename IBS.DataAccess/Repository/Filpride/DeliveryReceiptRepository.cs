using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class DeliveryReceiptRepository : Repository<FilprideDeliveryReceipt>, IDeliveryReceiptRepository
    {
        private ApplicationDbContext _db;

        public DeliveryReceiptRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilprideDeliveryReceipt? lastDr = await _db
                .FilprideDeliveryReceipts
                .OrderBy(c => c.DeliveryReceiptNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastDr != null)
            {
                string lastSeries = lastDr.DeliveryReceiptNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "DR0000000001";
            }
        }

        public override async Task<IEnumerable<FilprideDeliveryReceipt>> GetAllAsync(Expression<Func<FilprideDeliveryReceipt, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideDeliveryReceipt> query = dbSet
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(po => po.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po.Supplier)
                .Include(dr => dr.Hauler)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PickUpPoint)
                .Include(dr => dr.Customer)
                .Include(dr => dr.PurchaseOrder);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilprideDeliveryReceipt> GetAsync(Expression<Func<FilprideDeliveryReceipt, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(po => po.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po.Supplier)
                .Include(dr => dr.Hauler)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PickUpPoint)
                .Include(dr => dr.Customer)
                .Include(dr => dr.PurchaseOrder)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(dr => dr.DeliveryReceiptId == viewModel.DeliverReceiptId, cancellationToken);

            #region--Update COS

            await UpdateCosRemainingVolumeAsync(existingRecord.CustomerOrderSlipId, (viewModel.Volume - existingRecord.Quantity), cancellationToken);

            #endregion

            existingRecord.Date = viewModel.Date;
            existingRecord.EstimatedTimeOfArrival = viewModel.ETA;
            existingRecord.CustomerOrderSlipId = viewModel.CustomerOrderSlipId;
            existingRecord.CustomerId = viewModel.CustomerId;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.Quantity = viewModel.Volume;
            existingRecord.TotalAmount = viewModel.TotalAmount;
            existingRecord.ManualDrNo = viewModel.ManualDrNo;
            existingRecord.Freight = viewModel.Freight;
            existingRecord.Demuragge = viewModel.Demuragge;
            existingRecord.ECC = viewModel.ECC;

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = viewModel.CurrentUser;
                existingRecord.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetDeliveryReceiptListAsync(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideDeliveryReceipts
                .OrderBy(dr => dr.DeliveryReceiptId)
                .Where(dr => dr.DeliveredDate != null)
                .Select(dr => new SelectListItem
                {
                    Value = dr.DeliveryReceiptId.ToString(),
                    Text = dr.DeliveryReceiptNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetDeliveryReceiptListForSalesInvoice(int cosId, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideDeliveryReceipts
                    .OrderBy(dr => dr.DeliveryReceiptId)
                    .Where(dr => dr.CustomerOrderSlipId == cosId && dr.DeliveredDate != null && !dr.HasAlreadyInvoiced)
                    .Select(dr => new SelectListItem
                    {
                        Value = dr.DeliveryReceiptId.ToString(),
                        Text = dr.DeliveryReceiptNo
                    })
                    .ToListAsync(cancellationToken);
        }

        public async Task PostAsync(FilprideDeliveryReceipt deliveryReceipt, CancellationToken cancellationToken = default)
        {
            try
            {
                #region General Ledger Book Recording

                var ledgers = new List<FilprideGeneralLedgerBook>();
                var (salesAcctNo, salesAcctTitle) = GetSalesAccountTitle(deliveryReceipt.CustomerOrderSlip.Product.ProductName);
                var (cogsAcctNo, cogsAcctTitle) = GetCogsAccountTitle(deliveryReceipt.CustomerOrderSlip.Product.ProductName);
                var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(deliveryReceipt.CustomerOrderSlip.Product.ProductName);
                var netOfVatAmount = ComputeNetOfVat(deliveryReceipt.TotalAmount);
                var vatAmount = ComputeVatAmount(netOfVatAmount);
                var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException("Account title '101010100' not found.");
                var arTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");
                var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010100") ?? throw new ArgumentException("Account title '202010100' not found.");
                var freightOutTitle = accountTitlesDto.Find(c => c.AccountNumber == "502012700") ?? throw new ArgumentException("Account title '502012700' not found.");

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = (DateOnly)deliveryReceipt.DeliveredDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                    AccountNo = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountNumber : arTradeTitle.AccountNumber,
                    AccountTitle = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountName : arTradeTitle.AccountName,
                    Debit = deliveryReceipt.TotalAmount,
                    Credit = 0,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate,
                    CustomerId = deliveryReceipt.CustomerOrderSlip.Terms != SD.Terms_Cod ? deliveryReceipt.CustomerId : null,
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = (DateOnly)deliveryReceipt.DeliveredDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                    AccountNo = salesAcctNo,
                    AccountTitle = salesAcctTitle,
                    Debit = 0,
                    Credit = netOfVatAmount,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = (DateOnly)deliveryReceipt.DeliveredDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                    AccountNo = vatOutputTitle.AccountNumber,
                    AccountTitle = vatOutputTitle.AccountName,
                    Debit = 0,
                    Credit = vatAmount,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = (DateOnly)deliveryReceipt.DeliveredDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                    AccountNo = cogsAcctNo,
                    AccountTitle = cogsAcctTitle,
                    Debit = ComputeNetOfVat(deliveryReceipt.PurchaseOrder.Price * deliveryReceipt.Quantity),
                    Credit = 0,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = (DateOnly)deliveryReceipt.DeliveredDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                    AccountNo = vatInputTitle.AccountNumber,
                    AccountTitle = vatInputTitle.AccountName,
                    Debit = ComputeVatAmount(ComputeNetOfVat(deliveryReceipt.PurchaseOrder.Price * deliveryReceipt.Quantity)),
                    Credit = 0,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = (DateOnly)deliveryReceipt.DeliveredDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                    AccountNo = arTradeTitle.AccountNumber,
                    AccountTitle = arTradeTitle.AccountName,
                    Debit = 0,
                    Credit = deliveryReceipt.PurchaseOrder.Price * deliveryReceipt.Quantity,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate,
                    SupplierId = deliveryReceipt.PurchaseOrder.Supplier.SupplierId
                });

                if (deliveryReceipt.Freight > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"} for Freight",
                        AccountNo = freightOutTitle.AccountNumber,
                        AccountTitle = freightOutTitle.AccountName,
                        Debit = ComputeNetOfVat(deliveryReceipt.Freight * deliveryReceipt.Quantity),
                        Credit = 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"} for Freight",
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountTitle = vatInputTitle.AccountName,
                        Debit = ComputeVatAmount(ComputeNetOfVat(deliveryReceipt.Freight * deliveryReceipt.Quantity)),
                        Credit = 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate
                    });
                }

                if (deliveryReceipt.ECC > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"} for ECC",
                        AccountNo = freightOutTitle.AccountNumber,
                        AccountTitle = freightOutTitle.AccountName,
                        Debit = ComputeNetOfVat(deliveryReceipt.ECC * deliveryReceipt.Quantity),
                        Credit = 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"} for ECC",
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountTitle = vatInputTitle.AccountName,
                        Debit = ComputeVatAmount(ComputeNetOfVat(deliveryReceipt.ECC * deliveryReceipt.Quantity)),
                        Credit = 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate
                    });
                }

                if (deliveryReceipt.Demuragge > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"} for Demurrage",
                        AccountNo = freightOutTitle.AccountNumber,
                        AccountTitle = freightOutTitle.AccountName,
                        Debit = ComputeNetOfVat(deliveryReceipt.Demuragge),
                        Credit = 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"} for Demurrage",
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountTitle = vatInputTitle.AccountName,
                        Debit = ComputeVatAmount(ComputeNetOfVat(deliveryReceipt.Demuragge)),
                        Credit = 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate
                    });
                }

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = (DateOnly)deliveryReceipt.DeliveredDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                    AccountNo = apTradeTitle.AccountNumber,
                    AccountTitle = apTradeTitle.AccountName,
                    Debit = 0,
                    Credit = ((deliveryReceipt.Freight + deliveryReceipt.ECC) * deliveryReceipt.Quantity) + deliveryReceipt.Demuragge,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate,
                    SupplierId = deliveryReceipt.HaulerId
                });

                if (!IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                #endregion General Ledger Book Recording

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        private async Task UpdateCosRemainingVolumeAsync(int cosId, decimal drVolume, CancellationToken cancellationToken)
        {
            var cos = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(po => po.CustomerOrderSlipId == cosId, cancellationToken) ?? throw new InvalidOperationException("No record found.");

            if (cos != null)
            {
                cos.DeliveredQuantity += drVolume;
                cos.BalanceQuantity -= drVolume;

                if (cos.BalanceQuantity <= 0)
                {
                    cos.IsDelivered = true;
                    cos.Status = nameof(CosStatus.Completed);
                }
            }
        }

        public async Task DeductTheVolumeToCos(int cosId, decimal drVolume, CancellationToken cancellationToken = default)
        {
            var cos = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(po => po.CustomerOrderSlipId == cosId, cancellationToken) ?? throw new InvalidOperationException("No record found.");

            if (cos != null)
            {
                cos.DeliveredQuantity -= drVolume;
                cos.BalanceQuantity += drVolume;
                cos.IsDelivered = false;
            }
        }
    }
}