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
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.Commissionee)
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
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.Commissionee)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(dr => dr.DeliveryReceiptId == viewModel.DeliverReceiptId, cancellationToken);

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

            #region--Update Multiple PO

            if (existingRecord.CustomerOrderSlip.HasMultiplePO)
            {
                if (viewModel.Volume != existingRecord.Quantity)
                {
                    await UpdatePreviousAppointedSupplierAsync(existingRecord);
                    await AssignNewPurchaseOrderAsync(viewModel, existingRecord);
                }
            }

            #endregion

            existingRecord.Date = viewModel.Date;
            existingRecord.EstimatedTimeOfArrival = viewModel.ETA;
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
            existingRecord.AuthorityToLoadNo = customerOrderSlip.AuthorityToLoadNo;

            if (!customerOrderSlip.HasMultiplePO)
            {
                existingRecord.PurchaseOrderId = customerOrderSlip.PurchaseOrderId;
            }
            else
            {
                var selectedPo = await _db.FilprideCOSAppointedSuppliers
                    .OrderBy(s => s.PurchaseOrderId)
                    .FirstOrDefaultAsync(s => s.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId && !s.IsAssignedToDR);

                existingRecord.PurchaseOrderId = selectedPo.PurchaseOrderId;
            }

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
                    .Where(dr =>
                        dr.CustomerOrderSlipId == cosId &&
                        dr.DeliveredDate != null &&
                        !dr.HasAlreadyInvoiced &&
                        dr.Status == nameof(DRStatus.Delivered))
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
                var netOfVatAmount = ComputeNetOfVat(deliveryReceipt.TotalAmount);
                var vatAmount = ComputeVatAmount(netOfVatAmount);
                var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException("Account title '101010100' not found.");
                var arTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");
                var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010100") ?? throw new ArgumentException("Account title '202010100' not found.");
                var apHaulingPayableTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010300") ?? throw new ArgumentException("Account title '201010300' not found.");
                var apCommissionPayableTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010200") ?? throw new ArgumentException("Account title '201010200' not found.");
                var ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
                var ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                var ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");

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

                var cogsGrossAmount = deliveryReceipt.PurchaseOrder.Price * deliveryReceipt.Quantity;
                var cogsNetOfVat = ComputeNetOfVat(cogsGrossAmount);
                var cogsVatAmount = ComputeVatAmount(cogsNetOfVat);
                var cogsEwtAmount = ComputeEwtAmount(cogsVatAmount, 0.01m);
                var cogsNetOfEwt = ComputeNetOfEwt(cogsGrossAmount, cogsEwtAmount);

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = (DateOnly)deliveryReceipt.DeliveredDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                    AccountNo = cogsAcctNo,
                    AccountTitle = cogsAcctTitle,
                    Debit = cogsNetOfVat,
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
                    Debit = cogsVatAmount,
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
                    AccountNo = ewtOnePercent.AccountNumber,
                    AccountTitle = ewtOnePercent.AccountName,
                    Debit = 0,
                    Credit = cogsEwtAmount,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = (DateOnly)deliveryReceipt.DeliveredDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
                    AccountNo = apTradeTitle.AccountNumber,
                    AccountTitle = apTradeTitle.AccountName,
                    Debit = 0,
                    Credit = cogsNetOfEwt,
                    Company = deliveryReceipt.Company,
                    CreatedBy = deliveryReceipt.CreatedBy,
                    CreatedDate = deliveryReceipt.CreatedDate,
                    SupplierId = deliveryReceipt.PurchaseOrder.Supplier.SupplierId
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
                            AccountNo = freightAcctNo,
                            AccountTitle = freightAcctTitle,
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
                            AccountNo = freightAcctNo,
                            AccountTitle = freightAcctTitle,
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

                    var totalFreightGrossAmount = (deliveryReceipt.Freight + deliveryReceipt.ECC) * deliveryReceipt.Quantity;
                    var totalFreightNetOfVat = ComputeNetOfVat(totalFreightGrossAmount);
                    var totalFreightEwtAmount = ComputeEwtAmount(totalFreightNetOfVat, 0.02m);
                    var totalFreightNetOfEwt = ComputeNetOfEwt(totalFreightGrossAmount, totalFreightEwtAmount);


                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}",
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
                        AccountNo = ewtTwoPercent.AccountNumber,
                        AccountTitle = ewtTwoPercent.AccountName,
                        Debit = 0,
                        Credit = totalFreightEwtAmount,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate
                    });
                }

                if (deliveryReceipt.CustomerOrderSlip.CommissionRate > 0)
                {
                    var commissionGrossAmount = deliveryReceipt.CustomerOrderSlip.CommissionRate * deliveryReceipt.Quantity;
                    var commissionEwtAmount = deliveryReceipt.CustomerOrderSlip.Commissionee.TaxType == SD.TaxType_WithTax ?
                        ComputeEwtAmount(commissionGrossAmount, 0.05m) : 0;
                    var commissionNetOfEwt = deliveryReceipt.CustomerOrderSlip.Commissionee.TaxType == SD.TaxType_WithTax ?
                        ComputeNetOfEwt(commissionGrossAmount, commissionEwtAmount) : commissionGrossAmount;

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}.",
                        AccountNo = commissionAcctNo,
                        AccountTitle = commissionAcctTitle,
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
                        AccountNo = apCommissionPayableTitle.AccountNumber,
                        AccountTitle = apCommissionPayableTitle.AccountName,
                        Debit = 0,
                        Credit = commissionNetOfEwt,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.CreatedBy,
                        CreatedDate = deliveryReceipt.CreatedDate,
                        SupplierId = deliveryReceipt.CustomerOrderSlip.CommissioneeId
                    });

                    if (commissionEwtAmount > 0)
                    {
                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = (DateOnly)deliveryReceipt.DeliveredDate,
                            Reference = deliveryReceipt.DeliveryReceiptNo,
                            Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}.",
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
        }

        public async Task DeductTheVolumeToCos(int cosId, decimal drVolume, CancellationToken cancellationToken = default)
        {
            var cos = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(po => po.CustomerOrderSlipId == cosId, cancellationToken) ?? throw new InvalidOperationException("No record found.");

            if (cos.Status == nameof(CosStatus.Completed))
            {
                cos.Status = nameof(CosStatus.Approved);
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
            previousAppointedSupplier.IsAssignedToDR = false;
        }

        public async Task AssignNewPurchaseOrderAsync(DeliveryReceiptViewModel viewModel, FilprideDeliveryReceipt model)
        {
            var newAppointedSupplier = await _db.FilprideCOSAppointedSuppliers
                .OrderBy(s => s.PurchaseOrderId)
                .FirstOrDefaultAsync(s => s.CustomerOrderSlipId == viewModel.CustomerOrderSlipId &&
                                           !s.IsAssignedToDR &&
                                           s.Quantity == viewModel.Volume)
                ?? throw new InvalidOperationException($"Purchase Order not found for this volume ({viewModel.Volume:N2}), contact the TNS.");

            model.PurchaseOrderId = newAppointedSupplier.PurchaseOrderId;
            model.Quantity = viewModel.Volume;

            newAppointedSupplier.UnservedQuantity -= model.Quantity;
            newAppointedSupplier.IsAssignedToDR = true;
        }
    }
}
