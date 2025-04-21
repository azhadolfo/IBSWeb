using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class DeliveryReceiptRepository : Repository<FilprideDeliveryReceipt>, IDeliveryReceiptRepository
    {
        private ApplicationDbContext _db;

        public DeliveryReceiptRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string companyClaims, CancellationToken cancellationToken = default)
        {
            FilprideDeliveryReceipt? lastDr = await _db
                .FilprideDeliveryReceipts
                .Where(c => c.Company  == companyClaims)
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
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.Commissionee)
                .Include(dr => dr.PurchaseOrder).ThenInclude(po => po.Product);

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
                .Include(dr => dr.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.Commissionee)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(dr => dr.DeliveryReceiptId == viewModel.DeliveryReceiptId, cancellationToken);

            var customerOrderSlip = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);


            #region--Update COS

            if (viewModel.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId)
            {
                await UpdateCosRemainingVolumeAsync(existingRecord.CustomerOrderSlipId, (viewModel.Volume - existingRecord.Quantity), cancellationToken);
            }
            else
            {
                await DeductTheVolumeToCos(existingRecord.CustomerOrderSlipId, existingRecord.Quantity, cancellationToken);
            }

            #endregion

            #region--Update Appointed PO

            await UpdatePreviousAppointedSupplierAsync(existingRecord);
            await AssignNewPurchaseOrderAsync(viewModel, existingRecord);

            #endregion

            existingRecord.Date = viewModel.Date;
            existingRecord.CustomerOrderSlipId = viewModel.CustomerOrderSlipId;
            existingRecord.CustomerId = viewModel.CustomerId;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.Quantity = viewModel.Volume;
            existingRecord.TotalAmount = viewModel.TotalAmount;
            existingRecord.ManualDrNo = viewModel.ManualDrNo;
            existingRecord.Driver = viewModel.Driver;
            existingRecord.PlateNo = viewModel.PlateNo;
            existingRecord.HaulerId = viewModel.HaulerId ?? customerOrderSlip.HaulerId;
            existingRecord.ECC = viewModel.ECC;
            existingRecord.Freight = viewModel.Freight;
            existingRecord.FreightAmount = existingRecord.Quantity * (existingRecord.Freight + existingRecord.ECC);
            existingRecord.AuthorityToLoadNo = viewModel.ATLNo;
            existingRecord.CommissioneeId = customerOrderSlip.CommissioneeId;
            existingRecord.CommissionRate = customerOrderSlip.CommissionRate;
            existingRecord.CommissionAmount = existingRecord.Quantity * existingRecord.CommissionRate;
            existingRecord.CustomerAddress = customerOrderSlip.CustomerAddress;
            existingRecord.CustomerTin = customerOrderSlip.CustomerTin;

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = viewModel.CurrentUser;
                existingRecord.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                FilprideAuditTrail auditTrailBook = new(existingRecord.EditedBy, $"Edit delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", "", existingRecord.Company);
                await _db.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetDeliveryReceiptListAsync(string companyClaims, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideDeliveryReceipts
                .OrderBy(dr => dr.DeliveryReceiptId)
                .Where(dr => dr.DeliveredDate != null &&
                             dr.Company == companyClaims)
                .Select(dr => new SelectListItem
                {
                    Value = dr.DeliveryReceiptId.ToString(),
                    Text = dr.DeliveryReceiptNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetDeliveryReceiptListForSalesInvoice(string companyClaims, int cosId, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideDeliveryReceipts
                    .OrderBy(dr => dr.DeliveryReceiptId)
                    .Where(dr =>
                        dr.CustomerOrderSlipId == cosId &&
                        dr.DeliveredDate != null &&
                        !dr.HasAlreadyInvoiced &&
                        dr.Status == nameof(DRStatus.ForInvoicing) &&
                        dr.Company == companyClaims)
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
                var (salesAcctNo, salesAcctTitle) = GetSalesAccountTitle(deliveryReceipt.CustomerOrderSlip.Product.ProductCode);
                var (cogsAcctNo, cogsAcctTitle) = GetCogsAccountTitle(deliveryReceipt.CustomerOrderSlip.Product.ProductCode);
                var (freightAcctNo, freightAcctTitle) = GetFreightAccount(deliveryReceipt.CustomerOrderSlip.Product.ProductCode);
                var (commissionAcctNo, commissionAcctTitle) = GetCommissionAccount(deliveryReceipt.CustomerOrderSlip.Product.ProductCode);
                var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(deliveryReceipt.PurchaseOrder.Product.ProductCode);
                var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
                var salesTitle = accountTitlesDto.Find(c => c.AccountNumber == salesAcctNo) ?? throw new ArgumentException($"Account title '{salesAcctNo}' not found.");
                var cogsTitle = accountTitlesDto.Find(c => c.AccountNumber == cogsAcctNo) ?? throw new ArgumentException($"Account title '{cogsAcctNo}' not found.");
                var freightTitle = accountTitlesDto.Find(c => c.AccountNumber == freightAcctNo) ?? throw new ArgumentException($"Account title '{freightAcctNo}' not found.");
                var commissionTitle = accountTitlesDto.Find(c => c.AccountNumber == commissionAcctNo) ?? throw new ArgumentException($"Account title '{commissionAcctNo}' not found.");
                var inventoryTitle = accountTitlesDto.Find(c => c.AccountNumber == inventoryAcctNo) ?? throw new ArgumentException($"Account title '{inventoryAcctNo}' not found.");
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException("Account title '101010100' not found.");
                var arTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");
                var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                var apHaulingPayableTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010300") ?? throw new ArgumentException("Account title '201010300' not found.");
                var apCommissionPayableTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010200") ?? throw new ArgumentException("Account title '201010200' not found.");
                var ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                var ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");
                var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account title '101020200' not found.");
                var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account title '101020300' not found.");

                var netOfVatAmount = ComputeNetOfVat(deliveryReceipt.TotalAmount);
                var vatAmount = ComputeVatAmount(netOfVatAmount);
                var arTradeCwtAmount = deliveryReceipt.Customer.WithHoldingTax ? ComputeEwtAmount(deliveryReceipt.TotalAmount, 0.01m) : 0m;
                var arTradeCwvAmount = deliveryReceipt.Customer.WithHoldingTax ? ComputeEwtAmount(deliveryReceipt.TotalAmount, 0.05m) : 0m;
                var netOfEwtAmount = deliveryReceipt.TotalAmount - (arTradeCwtAmount + arTradeCwvAmount);


                if (arTradeCwtAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                        AccountId = arTradeCwt.AccountId,
                        AccountNo = arTradeCwt.AccountNumber,
                        AccountTitle = arTradeCwt.AccountName,
                        Debit = arTradeCwtAmount,
                        Credit = 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate
                    });
                }

                if (arTradeCwvAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                        AccountId = arTradeCwv.AccountId,
                        AccountNo = arTradeCwv.AccountNumber,
                        AccountTitle = arTradeCwv.AccountName,
                        Debit = arTradeCwvAmount,
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
                    AccountId = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountId : arTradeTitle.AccountId,
                    AccountNo = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountNumber : arTradeTitle.AccountNumber,
                    AccountTitle = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountName : arTradeTitle.AccountName,
                    Debit = netOfEwtAmount,
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
                    AccountId = salesTitle.AccountId,
                    AccountNo = salesTitle.AccountNumber,
                    AccountTitle = salesTitle.AccountName,
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
                    AccountId = vatOutputTitle.AccountId,
                    AccountNo = vatOutputTitle.AccountNumber,
                    AccountTitle = vatOutputTitle.AccountName,
                    Debit = 0,
                    Credit = vatAmount,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate
                });

                var cogsGrossAmount = deliveryReceipt.PurchaseOrder.Price * deliveryReceipt.Quantity;

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = (DateOnly)deliveryReceipt.DeliveredDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                    AccountId = cogsTitle.AccountId,
                    AccountNo = cogsTitle.AccountNumber,
                    AccountTitle = cogsTitle.AccountName,
                    Debit = cogsGrossAmount,
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
                    AccountId = inventoryTitle.AccountId,
                    AccountNo = inventoryTitle.AccountNumber,
                    AccountTitle = inventoryTitle.AccountName,
                    Debit = 0,
                    Credit = cogsGrossAmount,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate,
                });

                if (deliveryReceipt.Freight > 0 || deliveryReceipt.ECC > 0)
                {
                    if (deliveryReceipt.Freight > 0)
                    {
                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = (DateOnly)deliveryReceipt.DeliveredDate,
                            Reference = deliveryReceipt.DeliveryReceiptNo,
                            Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"} for Freight",
                            AccountId = freightTitle.AccountId,
                            AccountNo = freightTitle.AccountNumber,
                            AccountTitle = freightTitle.AccountName,
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
                            AccountId = vatInputTitle.AccountId,
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
                            AccountId = freightTitle.AccountId,
                            AccountNo = freightTitle.AccountNumber,
                            AccountTitle = freightTitle.AccountName,
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
                            AccountId = vatInputTitle.AccountId,
                            AccountNo = vatInputTitle.AccountNumber,
                            AccountTitle = vatInputTitle.AccountName,
                            Debit = ComputeVatAmount(ComputeNetOfVat(deliveryReceipt.ECC * deliveryReceipt.Quantity)),
                            Credit = 0,
                            Company = deliveryReceipt.Company,
                            CreatedBy = deliveryReceipt.CreatedBy,
                            CreatedDate = deliveryReceipt.CreatedDate
                        });
                    }

                    var totalFreightGrossAmount = deliveryReceipt.FreightAmount;
                    var totalFreightNetOfVat = ComputeNetOfVat(totalFreightGrossAmount);
                    var totalFreightEwtAmount = ComputeEwtAmount(totalFreightNetOfVat, 0.02m);
                    var totalFreightNetOfEwt = ComputeNetOfEwt(totalFreightGrossAmount, totalFreightEwtAmount);


                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                        AccountId = apHaulingPayableTitle.AccountId,
                        AccountNo = apHaulingPayableTitle.AccountNumber,
                        AccountTitle = apHaulingPayableTitle.AccountName,
                        Debit = 0,
                        Credit = totalFreightNetOfEwt,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate,
                        SupplierId = deliveryReceipt.HaulerId
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                        AccountId = ewtTwoPercent.AccountId,
                        AccountNo = ewtTwoPercent.AccountNumber,
                        AccountTitle = ewtTwoPercent.AccountName,
                        Debit = 0,
                        Credit = totalFreightEwtAmount,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate
                    });
                }

                if (deliveryReceipt.CommissionRate > 0)
                {
                    var commissionGrossAmount = deliveryReceipt.CommissionAmount;
                    var commissionEwtAmount = deliveryReceipt.Commissionee.TaxType == SD.TaxType_WithTax ?
                        ComputeEwtAmount(commissionGrossAmount, 0.05m) : 0;
                    var commissionNetOfEwt = deliveryReceipt.Commissionee.TaxType == SD.TaxType_WithTax ?
                        ComputeNetOfEwt(commissionGrossAmount, commissionEwtAmount) : commissionGrossAmount;

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}.",
                        AccountId = commissionTitle.AccountId,
                        AccountNo = commissionTitle.AccountNumber,
                        AccountTitle = commissionTitle.AccountName,
                        Debit = commissionGrossAmount,
                        Credit = 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}.",
                        AccountId = apCommissionPayableTitle.AccountId,
                        AccountNo = apCommissionPayableTitle.AccountNumber,
                        AccountTitle = apCommissionPayableTitle.AccountName,
                        Debit = 0,
                        Credit = commissionNetOfEwt,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate,
                        SupplierId = deliveryReceipt.CommissioneeId
                    });

                    if (commissionEwtAmount > 0)
                    {
                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = (DateOnly)deliveryReceipt.DeliveredDate,
                            Reference = deliveryReceipt.DeliveryReceiptNo,
                            Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}.",
                            AccountId = ewtFivePercent.AccountId,
                            AccountNo = ewtFivePercent.AccountNumber,
                            AccountTitle = ewtFivePercent.AccountName,
                            Debit = 0,
                            Credit = commissionEwtAmount,
                            Company = deliveryReceipt.Company,
                            CreatedBy = deliveryReceipt.CreatedBy,
                            CreatedDate = deliveryReceipt.CreatedDate
                        });
                    }

                }

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

        private async Task  UpdateCosRemainingVolumeAsync(int cosId, decimal drVolume, CancellationToken cancellationToken)
        {
            var cos = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(po => po.CustomerOrderSlipId == cosId, cancellationToken) ?? throw new InvalidOperationException("No record found.");

            cos.DeliveredQuantity += drVolume;
            cos.BalanceQuantity -= drVolume;

            if (cos.BalanceQuantity <= 0)
            {
                cos.Status = nameof(CosStatus.Completed);
            }
            else if (cos.BalanceQuantity >= 0 && cos.Status == nameof(CosStatus.Completed))
            {
                cos.Status = nameof(CosStatus.ForDR);
            }
        }

        public async Task DeductTheVolumeToCos(int cosId, decimal drVolume, CancellationToken cancellationToken = default)
        {
            var cos = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(po => po.CustomerOrderSlipId == cosId, cancellationToken) ?? throw new InvalidOperationException("No record found.");

            if (cos.Status == nameof(CosStatus.Completed))
            {
                cos.Status = nameof(CosStatus.ForDR);
            }

            cos.DeliveredQuantity -= drVolume;
            cos.BalanceQuantity += drVolume;
            cos.IsDelivered = false;
        }

        public async Task UpdatePreviousAppointedSupplierAsync(FilprideDeliveryReceipt model)
        {
            var previousAppointedSupplier = await _db.FilprideCOSAppointedSuppliers
                .FirstOrDefaultAsync(a => a.CustomerOrderSlipId == model.CustomerOrderSlipId && a.PurchaseOrderId == model.PurchaseOrderId);

            if (previousAppointedSupplier == null)
                throw new InvalidOperationException("Previous appointed supplier not found.");

            previousAppointedSupplier.UnservedQuantity += model.Quantity;
        }

        public async Task AssignNewPurchaseOrderAsync(DeliveryReceiptViewModel viewModel, FilprideDeliveryReceipt model)
        {
            var newAppointedSupplier = await _db.FilprideCOSAppointedSuppliers
                .OrderBy(s => s.PurchaseOrderId)
                .FirstOrDefaultAsync(s =>
                    s.CustomerOrderSlipId == viewModel.CustomerOrderSlipId &&
                    s.PurchaseOrderId == viewModel.PurchaseOrderId)
                ?? throw new InvalidOperationException($"Purchase Order not found for this volume ({viewModel.Volume:N2}), contact the TNS.");

            model.PurchaseOrderId = newAppointedSupplier.PurchaseOrderId;
            model.Quantity = viewModel.Volume;

            newAppointedSupplier.UnservedQuantity -= model.Quantity;
        }

        public async Task AutoReversalEntryForInTransit(CancellationToken cancellationToken = default)
        {
            var today = DateTimeHelper.GetCurrentPhilippineTime();

            // Start of the current month
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // End of the previous month
            var endOfPreviousMonth = startOfMonth.AddDays(-1);

            var inTransits = await GetAllAsync(dr =>
                    dr.Date.Month == endOfPreviousMonth.Month &&
                    dr.Date.Year == endOfPreviousMonth.Year &&
                    dr.Status == nameof(DRStatus.PendingDelivery), cancellationToken);

            foreach (var dr in inTransits.OrderBy(dr => dr.DeliveryReceiptNo))
            {
                var productCode = dr.PurchaseOrder.Product.ProductCode;
                var productCostGrossAmount = dr.Quantity * dr.PurchaseOrder.Price;
                var productCostNetOfVatAmount = ComputeNetOfVat(productCostGrossAmount);
                var productCostVatAmount = ComputeVatAmount(productCostNetOfVatAmount);
                var productCostEwtAmount = ComputeEwtAmount(productCostNetOfVatAmount, 0.01m);
                var productCostNetOfEwt = ComputeNetOfEwt(productCostGrossAmount, productCostEwtAmount);
                var ledgers = new List<FilprideGeneralLedgerBook>();
                var journalBooks = new List<FilprideJournalBook>();
                var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
                var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(productCode);
                var inventoryTitle = accountTitlesDto.Find(c => c.AccountNumber == inventoryAcctNo) ?? throw new ArgumentException($"Account title '{inventoryAcctNo}' not found.");
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010100") ?? throw new ArgumentException("Account title '202010100' not found.");
                var ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");

                #region In-Transit Entries

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(endOfPreviousMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                    AccountId = inventoryTitle.AccountId,
                    AccountNo = inventoryTitle.AccountNumber,
                    AccountTitle = inventoryTitle.AccountName,
                    Debit = productCostNetOfVatAmount,
                    Credit = 0,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(endOfPreviousMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                    AccountId = vatInputTitle.AccountId,
                    AccountNo = vatInputTitle.AccountNumber,
                    AccountTitle = vatInputTitle.AccountName,
                    Debit = productCostVatAmount,
                    Credit = 0,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(endOfPreviousMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                    AccountId = apTradeTitle.AccountId,
                    AccountNo = apTradeTitle.AccountNumber,
                    AccountTitle = apTradeTitle.AccountName,
                    Debit = 0,
                    Credit = productCostNetOfEwt,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    SupplierId = dr.PurchaseOrder.SupplierId,
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(endOfPreviousMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                    AccountId = ewtOnePercent.AccountId,
                    AccountNo = ewtOnePercent.AccountNumber,
                    AccountTitle = ewtOnePercent.AccountName,
                    Debit = 0,
                    Credit = productCostEwtAmount,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                #endregion

                #region Auto Reversal Entries

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(startOfMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                    AccountId = inventoryTitle.AccountId,
                    AccountNo = inventoryTitle.AccountNumber,
                    AccountTitle = inventoryTitle.AccountName,
                    Debit = 0,
                    Credit = productCostNetOfVatAmount,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                journalBooks.Add(new FilprideJournalBook
                {
                    Date = DateOnly.FromDateTime(startOfMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                    AccountTitle = $"{inventoryAcctNo} {inventoryAcctTitle}",
                    Debit = 0,
                    Credit = productCostNetOfVatAmount,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(startOfMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                    AccountId = vatInputTitle.AccountId,
                    AccountNo = vatInputTitle.AccountNumber,
                    AccountTitle = vatInputTitle.AccountName,
                    Debit = 0,
                    Credit = productCostVatAmount,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                journalBooks.Add(new FilprideJournalBook
                {
                    Date = DateOnly.FromDateTime(startOfMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                    AccountTitle = $"{vatInputTitle.AccountNumber} {vatInputTitle.AccountName}",
                    Debit = 0,
                    Credit = productCostVatAmount,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(startOfMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                    AccountId = apTradeTitle.AccountId,
                    AccountNo = apTradeTitle.AccountNumber,
                    AccountTitle = apTradeTitle.AccountName,
                    Debit = productCostNetOfEwt,
                    Credit = 0,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    SupplierId = dr.PurchaseOrder.SupplierId,
                });

                journalBooks.Add(new FilprideJournalBook
                {
                    Date = DateOnly.FromDateTime(startOfMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                    AccountTitle = $"{apTradeTitle.AccountNumber} {apTradeTitle.AccountName}",
                    Debit = productCostNetOfEwt,
                    Credit = 0,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(startOfMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                    AccountId = ewtOnePercent.AccountId,
                    AccountNo = ewtOnePercent.AccountNumber,
                    AccountTitle = ewtOnePercent.AccountName,
                    Debit = productCostEwtAmount,
                    Credit = 0,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                journalBooks.Add(new FilprideJournalBook
                {
                    Date = DateOnly.FromDateTime(startOfMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                    AccountTitle = $"{ewtOnePercent.AccountNumber} {ewtOnePercent.AccountName}",
                    Debit = productCostEwtAmount,
                    Credit = 0,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                #endregion

                if (!IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
                await _db.FilprideJournalBooks.AddRangeAsync(journalBooks, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

            }

        }

        public async Task<bool> CheckIfManualDrNoExists(string manualDrNo)
        {
            return await _db.FilprideDeliveryReceipts
                .Where(dr => dr.CanceledBy == null && dr.VoidedBy == null)
                .AnyAsync(dr => dr.ManualDrNo == manualDrNo);
        }
    }
}
