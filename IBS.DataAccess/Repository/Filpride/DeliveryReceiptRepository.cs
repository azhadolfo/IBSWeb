﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;

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

            if (!customerOrderSlip.HasMultiplePO && existingRecord.CustomerOrderSlipId != customerOrderSlip.CustomerOrderSlipId)
            {
                existingRecord.PurchaseOrderId = customerOrderSlip.PurchaseOrderId;
            }
            else if (customerOrderSlip.HasMultiplePO && existingRecord.CustomerOrderSlipId != customerOrderSlip.CustomerOrderSlipId)
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
                        dr.Status == nameof(DRStatus.ForInvoicing))
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
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010100") ?? throw new ArgumentException("Account title '202010100' not found.");
                var apHaulingPayableTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010300") ?? throw new ArgumentException("Account title '201010300' not found.");
                var apCommissionPayableTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010200") ?? throw new ArgumentException("Account title '201010200' not found.");
                var ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
                var ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                var ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");

                #region In-Transit Entries

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(endOfPreviousMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                    AccountNo = inventoryAcctNo,
                    AccountTitle = inventoryAcctTitle,
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
                    AccountNo = ewtOnePercent.AccountNumber,
                    AccountTitle = ewtOnePercent.AccountName,
                    Debit = 0,
                    Credit = productCostEwtAmount,
                    Company = dr.Company,
                    CreatedBy = "SYSTEM GENERATED",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                });

                if (dr.Freight > 0 || dr.ECC > 0)
                {
                    if (dr.Freight > 0)
                    {
                        var freightGrossAmount = dr.Quantity * dr.Freight;
                        var freightNetOfVat = ComputeNetOfVat(freightGrossAmount);
                        var freightVatAmount = ComputeVatAmount(freightNetOfVat);

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = DateOnly.FromDateTime(endOfPreviousMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountNo = inventoryAcctNo,
                            AccountTitle = inventoryAcctTitle,
                            Debit = freightNetOfVat,
                            Credit = 0,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = DateOnly.FromDateTime(endOfPreviousMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountNo = vatInputTitle.AccountNumber,
                            AccountTitle = vatInputTitle.AccountName,
                            Debit = freightVatAmount,
                            Credit = 0,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });
                    }

                    if (dr.ECC > 0)
                    {
                        var eccGrossAmount = dr.Quantity * dr.ECC;
                        var eccNetOfVat = ComputeNetOfVat(eccGrossAmount);
                        var eccVatAmount = ComputeVatAmount(eccNetOfVat);

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = DateOnly.FromDateTime(endOfPreviousMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountNo = inventoryAcctNo,
                            AccountTitle = inventoryAcctTitle,
                            Debit = eccNetOfVat,
                            Credit = 0,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = DateOnly.FromDateTime(endOfPreviousMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountNo = vatInputTitle.AccountNumber,
                            AccountTitle = vatInputTitle.AccountName,
                            Debit = eccVatAmount,
                            Credit = 0,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });
                    }

                    var totalFreightGrossAmount = dr.Quantity * (dr.Freight + dr.ECC);
                    var totalFreightNetOfVat = ComputeNetOfVat(totalFreightGrossAmount);
                    var totalFreightEwtAmount = ComputeEwtAmount(totalFreightNetOfVat, 0.02m);
                    var totalFreightNetOfEwt = ComputeNetOfEwt(totalFreightGrossAmount, totalFreightEwtAmount);

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(endOfPreviousMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                        AccountNo = apHaulingPayableTitle.AccountNumber,
                        AccountTitle = apHaulingPayableTitle.AccountName,
                        Debit = 0,
                        Credit = totalFreightNetOfEwt,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SupplierId = dr.HaulerId
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(endOfPreviousMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                        AccountNo = ewtTwoPercent.AccountNumber,
                        AccountTitle = ewtTwoPercent.AccountName,
                        Debit = 0,
                        Credit = totalFreightEwtAmount,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });

                }

                if (dr.CustomerOrderSlip.CommissionRate > 0)
                {
                    var totalCommissionGrossAmount = dr.Quantity * dr.CustomerOrderSlip.CommissionRate;
                    var totalCommissionNetOfVat = ComputeNetOfVat(totalCommissionGrossAmount);
                    var totalCommissionVatAmount = ComputeVatAmount(totalCommissionNetOfVat);
                    var totalCommissionEwtAmount = ComputeEwtAmount(totalCommissionNetOfVat, 0.05m);
                    var totalCommissionNetOfEwt = ComputeNetOfEwt(totalCommissionGrossAmount, totalCommissionEwtAmount);

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(endOfPreviousMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy} for commission.",
                        AccountNo = inventoryAcctNo,
                        AccountTitle = inventoryAcctTitle,
                        Debit = totalCommissionGrossAmount,
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
                        AccountNo = apCommissionPayableTitle.AccountNumber,
                        AccountTitle = apCommissionPayableTitle.AccountName,
                        Debit = 0,
                        Credit = totalCommissionNetOfEwt,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SupplierId = dr.CustomerOrderSlip.CommissioneeId
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(endOfPreviousMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                        AccountNo = ewtFivePercent.AccountNumber,
                        AccountTitle = ewtTwoPercent.AccountName,
                        Debit = 0,
                        Credit = totalCommissionEwtAmount,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });
                }

                #endregion

                #region Auto Reversal Entries

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(startOfMonth),
                    Reference = dr.DeliveryReceiptNo,
                    Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                    AccountNo = inventoryAcctNo,
                    AccountTitle = inventoryAcctTitle,
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

                if (dr.Freight > 0 || dr.ECC > 0)
                {
                    if (dr.Freight > 0)
                    {
                        var freightGrossAmount = dr.Quantity * dr.Freight;
                        var freightNetOfVat = ComputeNetOfVat(freightGrossAmount);
                        var freightVatAmount = ComputeVatAmount(freightNetOfVat);

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = DateOnly.FromDateTime(startOfMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountNo = inventoryAcctNo,
                            AccountTitle = inventoryAcctTitle,
                            Debit = 0,
                            Credit = freightNetOfVat,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        journalBooks.Add(new FilprideJournalBook
                        {
                            Date = DateOnly.FromDateTime(startOfMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountTitle = $"{inventoryAcctNo} {inventoryAcctTitle}",
                            Debit = 0,
                            Credit = freightNetOfVat,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = DateOnly.FromDateTime(startOfMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountNo = vatInputTitle.AccountNumber,
                            AccountTitle = vatInputTitle.AccountName,
                            Debit = 0,
                            Credit = freightVatAmount,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        journalBooks.Add(new FilprideJournalBook
                        {
                            Date = DateOnly.FromDateTime(startOfMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountTitle = $"{vatInputTitle.AccountNumber} {vatInputTitle.AccountName}",
                            Debit = 0,
                            Credit = freightVatAmount,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });
                    }

                    if (dr.ECC > 0)
                    {
                        var eccGrossAmount = dr.Quantity * dr.ECC;
                        var eccNetOfVat = ComputeNetOfVat(eccGrossAmount);
                        var eccVatAmount = ComputeVatAmount(eccNetOfVat);

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = DateOnly.FromDateTime(startOfMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountNo = inventoryAcctNo,
                            AccountTitle = inventoryAcctTitle,
                            Debit = 0,
                            Credit = eccNetOfVat,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        journalBooks.Add(new FilprideJournalBook
                        {
                            Date = DateOnly.FromDateTime(startOfMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountTitle = $"{inventoryAcctNo} {inventoryAcctTitle}",
                            Debit = 0,
                            Credit = eccNetOfVat,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = DateOnly.FromDateTime(startOfMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountNo = vatInputTitle.AccountNumber,
                            AccountTitle = vatInputTitle.AccountName,
                            Debit = 0,
                            Credit = eccVatAmount,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        journalBooks.Add(new FilprideJournalBook
                        {
                            Date = DateOnly.FromDateTime(startOfMonth),
                            Reference = dr.DeliveryReceiptNo,
                            Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy} for freight.",
                            AccountTitle = $"{vatInputTitle.AccountNumber} {vatInputTitle.AccountName}",
                            Debit = 0,
                            Credit = eccVatAmount,
                            Company = dr.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });
                    }

                    var totalFreightGrossAmount = dr.Quantity * (dr.Freight + dr.ECC);
                    var totalFreightNetOfVat = ComputeNetOfVat(totalFreightGrossAmount);
                    var totalFreightEwtAmount = ComputeEwtAmount(totalFreightNetOfVat, 0.02m);
                    var totalFreightNetOfEwt = ComputeNetOfEwt(totalFreightGrossAmount, totalFreightEwtAmount);

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(startOfMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                        AccountNo = apHaulingPayableTitle.AccountNumber,
                        AccountTitle = apHaulingPayableTitle.AccountName,
                        Debit = totalFreightNetOfEwt,
                        Credit = 0,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SupplierId = dr.HaulerId
                    });

                    journalBooks.Add(new FilprideJournalBook
                    {
                        Date = DateOnly.FromDateTime(startOfMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                        AccountTitle = $"{apHaulingPayableTitle.AccountNumber} {apHaulingPayableTitle.AccountName}",
                        Debit = totalFreightNetOfEwt,
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
                        AccountNo = ewtTwoPercent.AccountNumber,
                        AccountTitle = ewtTwoPercent.AccountName,
                        Debit = totalFreightEwtAmount,
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
                        AccountTitle = $"{ewtTwoPercent.AccountNumber} {ewtTwoPercent.AccountName}",
                        Debit = totalFreightEwtAmount,
                        Credit = 0,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });

                }

                if (dr.CustomerOrderSlip.CommissionRate > 0)
                {
                    var totalCommissionGrossAmount = dr.Quantity * dr.CustomerOrderSlip.CommissionRate;
                    var totalCommissionNetOfVat = ComputeNetOfVat(totalCommissionGrossAmount);
                    var totalCommissionVatAmount = ComputeVatAmount(totalCommissionNetOfVat);
                    var totalCommissionEwtAmount = ComputeEwtAmount(totalCommissionNetOfVat, 0.05m);
                    var totalCommissionNetOfEwt = ComputeNetOfEwt(totalCommissionGrossAmount, totalCommissionEwtAmount);

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(startOfMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                        AccountNo = inventoryAcctNo,
                        AccountTitle = inventoryAcctTitle,
                        Debit = 0,
                        Credit = totalCommissionGrossAmount,
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
                        Credit = totalCommissionGrossAmount,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(startOfMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                        AccountNo = apCommissionPayableTitle.AccountNumber,
                        AccountTitle = apCommissionPayableTitle.AccountName,
                        Debit = totalCommissionNetOfEwt,
                        Credit = 0,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SupplierId = dr.CustomerOrderSlip.CommissioneeId
                    });

                    journalBooks.Add(new FilprideJournalBook
                    {
                        Date = DateOnly.FromDateTime(startOfMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                        AccountTitle = $"{apCommissionPayableTitle.AccountNumber} {apCommissionPayableTitle.AccountName}",
                        Debit = totalCommissionNetOfEwt,
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
                        AccountNo = ewtFivePercent.AccountNumber,
                        AccountTitle = ewtTwoPercent.AccountName,
                        Debit = totalCommissionEwtAmount,
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
                        AccountTitle = $"{ewtFivePercent.AccountNumber} {ewtTwoPercent.AccountName}",
                        Debit = totalCommissionEwtAmount,
                        Credit = 0,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });
                }

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
    }
}
