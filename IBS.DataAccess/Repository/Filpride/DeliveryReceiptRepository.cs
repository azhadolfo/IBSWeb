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
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Supplier)
                .Include(dr => dr.Hauler)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PickUpPoint)
                .Include(dr => dr.Customer);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilprideDeliveryReceipt> GetAsync(Expression<Func<FilprideDeliveryReceipt, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Supplier)
                .Include(dr => dr.Hauler)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PickUpPoint)
                .Include(dr => dr.Customer)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(dr => dr.DeliveryReceiptId == viewModel.DeliverReceiptId, cancellationToken);

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
            existingRecord.Driver = viewModel.Driver;
            existingRecord.PlateNo = viewModel.PlateNo;
            existingRecord.HaulerId = viewModel.HaulerId;

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
                .Where(dr => dr.PostedBy != null && dr.DeliveredDate != null)
                .Select(dr => new SelectListItem
                {
                    Value = dr.DeliveryReceiptId.ToString(),
                    Text = dr.DeliveryReceiptNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetDeliveryReceiptListByCos(int cosId, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideDeliveryReceipts
                    .OrderBy(dr => dr.DeliveryReceiptId)
                    .Where(dr => dr.PostedBy != null && dr.CustomerOrderSlipId == cosId && dr.DeliveredDate != null)
                    .Select(dr => new SelectListItem
                    {
                        Value = dr.DeliveryReceiptId.ToString(),
                        Text = dr.DeliveryReceiptNo
                    })
                    .ToListAsync(cancellationToken);
        }

        public async Task PostAsync(FilprideDeliveryReceipt deliveryReceipt, CancellationToken cancellationToken = default)
        {
            #region--Update COS

            await UpdateCosRemainingVolumeAsync(deliveryReceipt.CustomerOrderSlipId, deliveryReceipt.Quantity, cancellationToken);

            #endregion

            #region General Ledger Book Recording

            var ledgers = new List<FilprideGeneralLedgerBook>();
            var (salesAcctNo, salesAcctTitle) = GetSalesAccountTitle(deliveryReceipt.CustomerOrderSlip.PurchaseOrder.Product.ProductName);
            var (cogsAcctNo, cogsAcctTitle) = GetCogsAccountTitle(deliveryReceipt.CustomerOrderSlip.PurchaseOrder.Product.ProductName);
            var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(deliveryReceipt.CustomerOrderSlip.PurchaseOrder.Product.ProductName);
            var netOfVatAmount = ComputeNetOfVat(deliveryReceipt.TotalAmount);
            var vatAmount = ComputeVatAmount(netOfVatAmount);

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = (DateOnly)deliveryReceipt.DeliveredDate,
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
                AccountNo = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? "1010201" : "1010101",
                AccountTitle = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? "Cash in Bank" : "AR-Trade Receivable",
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
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
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
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
                AccountNo = "2010301",
                AccountTitle = "Vat Output",
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
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
                AccountNo = cogsAcctNo,
                AccountTitle = cogsAcctTitle,
                Debit = ComputeNetOfVat(deliveryReceipt.CustomerOrderSlip.PurchaseOrder.Price * deliveryReceipt.Quantity),
                Credit = 0,
                Company = deliveryReceipt.Company,
                CreatedBy = deliveryReceipt.CreatedBy,
                CreatedDate = deliveryReceipt.CreatedDate
            });

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = (DateOnly)deliveryReceipt.DeliveredDate,
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
                AccountNo = "1010602",
                AccountTitle = "Vat Input",
                Debit = ComputeVatAmount(ComputeNetOfVat(deliveryReceipt.CustomerOrderSlip.PurchaseOrder.Price * deliveryReceipt.Quantity)),
                Credit = 0,
                Company = deliveryReceipt.Company,
                CreatedBy = deliveryReceipt.CreatedBy,
                CreatedDate = deliveryReceipt.CreatedDate
            });

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = (DateOnly)deliveryReceipt.DeliveredDate,
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
                AccountNo = "2010101",
                AccountTitle = "Accounts Payables - Trade",
                Debit = 0,
                Credit = deliveryReceipt.CustomerOrderSlip.PurchaseOrder.Price * deliveryReceipt.Quantity,
                Company = deliveryReceipt.Company,
                CreatedBy = deliveryReceipt.CreatedBy,
                CreatedDate = deliveryReceipt.CreatedDate,
                SupplierId = deliveryReceipt.CustomerOrderSlip.PurchaseOrder.SupplierId
            });

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = (DateOnly)deliveryReceipt.DeliveredDate,
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
                AccountNo = "5010109",
                AccountTitle = "COGS - Freight",
                Debit = ComputeNetOfVat((decimal)deliveryReceipt.CustomerOrderSlip.Freight * deliveryReceipt.Quantity),
                Credit = 0,
                Company = deliveryReceipt.Company,
                CreatedBy = deliveryReceipt.CreatedBy,
                CreatedDate = deliveryReceipt.CreatedDate
            });

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = (DateOnly)deliveryReceipt.DeliveredDate,
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
                AccountNo = "1010602",
                AccountTitle = "Vat Input",
                Debit = ComputeVatAmount(ComputeNetOfVat((decimal)deliveryReceipt.CustomerOrderSlip.Freight * deliveryReceipt.Quantity)),
                Credit = 0,
                Company = deliveryReceipt.Company,
                CreatedBy = deliveryReceipt.CreatedBy,
                CreatedDate = deliveryReceipt.CreatedDate
            });

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = (DateOnly)deliveryReceipt.DeliveredDate,
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
                AccountNo = "5010108",
                AccountTitle = "COGS - Demurrage",
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
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
                AccountNo = "1010602",
                AccountTitle = "Vat Input",
                Debit = ComputeVatAmount(ComputeNetOfVat(deliveryReceipt.Demuragge)),
                Credit = 0,
                Company = deliveryReceipt.Company,
                CreatedBy = deliveryReceipt.CreatedBy,
                CreatedDate = deliveryReceipt.CreatedDate
            });

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = (DateOnly)deliveryReceipt.DeliveredDate,
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler.SupplierName}",
                AccountNo = "2010101",
                AccountTitle = "Accounts Payables - Trade",
                Debit = 0,
                Credit = ((decimal)deliveryReceipt.CustomerOrderSlip.Freight * deliveryReceipt.Quantity) + deliveryReceipt.Demuragge,
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

            #endregion

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateCosRemainingVolumeAsync(int cosId, decimal drVolume, CancellationToken cancellationToken)
        {
            var cos = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(po => po.CustomerOrderSlipId == cosId, cancellationToken) ?? throw new InvalidOperationException("No record found.");

            if (cos != null)
            {
                cos.DeliveredQuantity = drVolume;
                cos.BalanceQuantity -= cos.DeliveredQuantity;

                if (cos.BalanceQuantity <= 0)
                {
                    cos.IsDelivered = true;
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