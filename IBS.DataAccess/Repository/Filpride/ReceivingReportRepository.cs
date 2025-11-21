using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ReceivingReportRepository : Repository<FilprideReceivingReport>, IReceivingReportRepository
    {
        private readonly ApplicationDbContext _db;

        public ReceivingReportRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(company, cancellationToken);
            }

            return await GenerateCodeForUnDocumented(company, cancellationToken);
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastRr = await _db
                .FilprideReceivingReports
                .Where(rr => rr.Company == company && !rr.ReceivingReportNo!.StartsWith("RRBEG") && rr.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr == null)
            {
                return "RR0000000001";
            }

            var lastSeries = lastRr.ReceivingReportNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastRr = await _db
                .FilprideReceivingReports
                .Where(rr => rr.Company == company && !rr.ReceivingReportNo!.StartsWith("RRBEG") && rr.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr == null)
            {
                return "RRU000000001";
            }

            var lastSeries = lastRr.ReceivingReportNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

        }

        public async Task<int> RemoveQuantityReceived(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _db.FilpridePurchaseOrders
                .Include(po => po.ActualPrices)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (po == null)
            {
                throw new ArgumentException("No record found.");
            }

            po.QuantityReceived -= quantityReceived;

            if (po.IsReceived)
            {
                po.IsReceived = false;
                po.ReceivedDate = DateTime.MaxValue;
            }

            if (po.ActualPrices!.Count <= 0)
            {
                return await _db.SaveChangesAsync(cancellationToken);
            }

            po.ActualPrices.FirstOrDefault()!.AppliedVolume -= quantityReceived;;
            return await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdatePoAsync(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _db.FilpridePurchaseOrders
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (po != null)
            {
                po.QuantityReceived += quantityReceived;

                if (po.QuantityReceived == po.Quantity)
                {
                    po.IsReceived = true;
                    po.ReceivedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    await _db.SaveChangesAsync(cancellationToken);
                }
                if (po.QuantityReceived > po.Quantity)
                {
                    throw new ArgumentException("Input is exceed to remaining quantity received");
                }
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public override async Task<FilprideReceivingReport?> GetAsync(Expression<Func<FilprideReceivingReport, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr!.Customer)
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideReceivingReport>> GetAllAsync(Expression<Func<FilprideReceivingReport, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideReceivingReport> query = dbSet
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr!.Customer)
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po!.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<string> AutoGenerateReceivingReport(FilprideDeliveryReceipt deliveryReceipt, DateOnly liftingDate, string userName, CancellationToken cancellationToken = default)
        {
            FilprideReceivingReport model = new()
            {
                DeliveryReceiptId = deliveryReceipt.DeliveryReceiptId,
                Date = liftingDate,
                POId = deliveryReceipt.PurchaseOrder!.PurchaseOrderId,
                PONo = deliveryReceipt.PurchaseOrder.PurchaseOrderNo,
                QuantityDelivered = deliveryReceipt.Quantity,
                QuantityReceived = deliveryReceipt.Quantity,
                TruckOrVessels = deliveryReceipt.CustomerOrderSlip!.PickUpPoint!.Depot,
                AuthorityToLoadNo = deliveryReceipt.AuthorityToLoadNo,
                Remarks = "PENDING",
                Company = deliveryReceipt.Company,
                CreatedBy = userName,
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                PostedBy = userName,
                PostedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                Status = nameof(Status.Posted),
                Type = deliveryReceipt.PurchaseOrder.Type,
                TaxPercentage = deliveryReceipt.PurchaseOrder!.Supplier!.WithholdingTaxPercent ?? 0m
            };

            if (model.QuantityDelivered > deliveryReceipt.PurchaseOrder.Quantity - deliveryReceipt.PurchaseOrder.QuantityReceived)
            {
                throw new ArgumentException($"The inputted quantity exceeds the remaining balance for Purchase Order: " +
                                            $"{deliveryReceipt.PurchaseOrder.PurchaseOrderNo}.");
            }

            var freight = deliveryReceipt.CustomerOrderSlip.DeliveryOption == SD.DeliveryOption_DirectDelivery
                ? (decimal)deliveryReceipt.CustomerOrderSlip!.Freight!
                : 0;

            model.ReceivedDate = model.Date;
            model.ReceivingReportNo = await GenerateCodeAsync(model.Company, model.Type!, cancellationToken);
            model.DueDate = await ComputeDueDateAsync(deliveryReceipt.PurchaseOrder.Terms, model.Date, cancellationToken);
            model.GainOrLoss = model.QuantityDelivered - model.QuantityReceived;

            var poActualPrice = await _db.FilpridePOActualPrices
                .FirstOrDefaultAsync(a => a.PurchaseOrderId == deliveryReceipt.PurchaseOrderId
                                          && a.IsApproved
                                          && a.AppliedVolume != a.TriggeredVolume,
                    cancellationToken);

            var remainingQuantity = model.QuantityReceived;
            decimal totalAmount = 0;

            if (poActualPrice != null)
            {
                var availableQuantity = poActualPrice.TriggeredVolume - poActualPrice.AppliedVolume;

                // Compute using poActualPrice.Price for the available quantity
                if (availableQuantity > 0)
                {
                    var applicableQuantity = Math.Min(remainingQuantity, availableQuantity);
                    totalAmount += applicableQuantity * (poActualPrice.TriggeredPrice + freight);
                    poActualPrice.AppliedVolume += applicableQuantity;
                    remainingQuantity -= applicableQuantity;
                }
            }

            // Compute the remaining using the default price
            totalAmount += remainingQuantity * ((poActualPrice?.TriggeredPrice != null ? poActualPrice!.TriggeredPrice : deliveryReceipt.PurchaseOrder.Price) + freight);
            model.Amount = totalAmount;

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailCreate = new(model.PostedBy,
                $"Created new receiving report# {model.ReceivingReportNo}",
                "Receiving Report",
                model.Company);

            FilprideAuditTrail auditTrailPost = new(model.PostedBy,
                $"Posted receiving report# {model.ReceivingReportNo}",
                "Receiving Report",
                model.Company);

            await _db.AddAsync(auditTrailCreate, cancellationToken);
            await _db.AddAsync(auditTrailPost, cancellationToken);

            #endregion --Audit Trail Recording

            await _db.AddAsync(model, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            #region Update the invoice if any

            var salesInvoice = await _db.FilprideSalesInvoices
                .FirstOrDefaultAsync(si => si.DeliveryReceiptId == model.DeliveryReceiptId, cancellationToken);

            if (salesInvoice != null)
            {
                salesInvoice.ReceivingReportId = model.ReceivingReportId;
            }

            #endregion

            await PostAsync(model, cancellationToken);

            return model.ReceivingReportNo;
        }

        public async Task PostAsync(FilprideReceivingReport model, CancellationToken cancellationToken = default)
        {
            #region --General Ledger Recording

            var ledgers = new List<FilprideGeneralLedgerBook>();

            var netOfVatAmount = model.PurchaseOrder!.VatType == SD.VatType_Vatable
                ? ComputeNetOfVat(model.Amount)
                : model.Amount;
            var vatAmount = model.PurchaseOrder.VatType == SD.VatType_Vatable
                ? ComputeVatAmount(netOfVatAmount)
                : 0m;
            var ewtAmount = model.PurchaseOrder!.TaxType == SD.TaxType_WithTax
                ? ComputeEwtAmount(netOfVatAmount, model.TaxPercentage)
                : 0m;

            var supplierTaxTitle = model.PurchaseOrder.Supplier!.WithholdingTaxTitle?.Split(" ", 2);

            if (model.PurchaseOrder.Terms == SD.Terms_Cod || model.PurchaseOrder.Terms == SD.Terms_Prepaid)
            {
                var advancesVoucher = await _db.FilprideCheckVoucherDetails
                    .Include(cv => cv.CheckVoucherHeader)
                    .FirstOrDefaultAsync(cv =>
                        cv.CheckVoucherHeader!.SupplierId == model.PurchaseOrder.SupplierId &&
                        cv.CheckVoucherHeader.IsAdvances &&
                        cv.CheckVoucherHeader.Status == nameof(CheckVoucherPaymentStatus.Posted) &&
                        cv.AccountName.Contains("Expanded Withholding Tax") &&
                        cv.Credit > cv.AmountPaid,
                        cancellationToken);

                if (advancesVoucher != null)
                {
                    var affectedEwt = Math.Min(advancesVoucher.Credit, ewtAmount);
                    ewtAmount -= affectedEwt;
                    advancesVoucher.AmountPaid += affectedEwt;
                }
            }

            var netOfEwtAmount = model.PurchaseOrder!.TaxType == SD.TaxType_WithTax
                ? ComputeNetOfEwt(model.Amount, ewtAmount)
                : model.Amount;

            var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(model.PurchaseOrder.Product!.ProductCode);
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200")
                                ?? throw new ArgumentException("Account title '101060200' not found.");
            var ewtAccountNo = supplierTaxTitle?.FirstOrDefault();
            var ewtTitle = accountTitlesDto.FirstOrDefault(c => c.AccountNumber == ewtAccountNo);
            var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010100")
                               ?? throw new ArgumentException("Account title '202010100' not found.");
            var inventoryTitle = accountTitlesDto.Find(c => c.AccountNumber == inventoryAcctNo)
                                 ?? throw new ArgumentException($"Account title '{inventoryAcctNo}' not found.");

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = model.Date,
                Reference = model.ReceivingReportNo!,
                Description = "Receipt of Goods",
                AccountId = inventoryTitle.AccountId,
                AccountNo = inventoryTitle.AccountNumber,
                AccountTitle = inventoryTitle.AccountName,
                Debit = netOfVatAmount,
                Credit = 0,
                CreatedBy = model.PostedBy,
                CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                Company = model.Company,
                ModuleType = nameof(ModuleType.Purchase)
            });

            if (vatAmount > 0)
            {
                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = model.Date,
                    Reference = model.ReceivingReportNo!,
                    Description = "Receipt of Goods",
                    AccountId = vatInputTitle.AccountId,
                    AccountNo = vatInputTitle.AccountNumber,
                    AccountTitle = vatInputTitle.AccountName,
                    Debit = vatAmount,
                    Credit = 0,
                    CreatedBy = model.PostedBy,
                    CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    Company = model.Company,
                    ModuleType = nameof(ModuleType.Purchase)
                });
            }

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = model.Date,
                Reference = model.ReceivingReportNo!,
                Description = "Receipt of Goods",
                AccountId = apTradeTitle.AccountId,
                AccountNo = apTradeTitle.AccountNumber,
                AccountTitle = apTradeTitle.AccountName,
                Debit = 0,
                Credit = netOfEwtAmount,
                CreatedBy = model.PostedBy,
                CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                Company = model.Company,
                SupplierId = model.PurchaseOrder.SupplierId,
                SupplierName = model.PurchaseOrder.SupplierName,
                ModuleType = nameof(ModuleType.Purchase)
            });

            if (ewtAmount > 0)
            {
                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = model.Date,
                    Reference = model.ReceivingReportNo!,
                    Description = "Receipt of Goods",
                    AccountId = ewtTitle!.AccountId,
                    AccountNo = ewtTitle.AccountNumber,
                    AccountTitle = ewtTitle.AccountName,
                    Debit = 0,
                    Credit = ewtAmount,
                    CreatedBy = model.PostedBy,
                    CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    Company = model.Company,
                    ModuleType = nameof(ModuleType.Purchase)
                });
            }

            if (!IsJournalEntriesBalanced(ledgers))
            {
                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
            }

            await _db.AddRangeAsync(ledgers, cancellationToken);

            #endregion --General Ledger Recording

            #region--Inventory Recording

            var unitOfWork = new UnitOfWork(_db);

            await unitOfWork.FilprideInventory.AddPurchaseToInventoryAsync(model, cancellationToken);

            #endregion

            await UpdatePoAsync(model.PurchaseOrder.PurchaseOrderId, model.QuantityReceived, cancellationToken);

            #region --Purchase Book Recording

            FilpridePurchaseBook purchaseBook = new()
            {
                Date = model.Date,
                SupplierName = model.PurchaseOrder.SupplierName,
                SupplierTin = model.PurchaseOrder.SupplierTin,
                SupplierAddress = model.PurchaseOrder.SupplierAddress,
                DocumentNo = model.ReceivingReportNo!,
                Description = model.PurchaseOrder.ProductName,
                Amount = model.Amount,
                VatAmount = vatAmount,
                WhtAmount = ewtAmount,
                NetPurchases = netOfVatAmount,
                CreatedBy = model.CreatedBy,
                PONo = model.PurchaseOrder.PurchaseOrderNo!,
                DueDate = model.DueDate,
                Company = model.Company
            };

            await _db.AddAsync(purchaseBook, cancellationToken);
            #endregion --Purchase Book Recording

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task VoidReceivingReportAsync(int receivingReportId, string currentUser, CancellationToken cancellationToken = default)
        {
            var model = await GetAsync(r => r.ReceivingReportId == receivingReportId, cancellationToken);

            var existingInventory = await _db.FilprideInventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Reference == model!.ReceivingReportNo && i.Company == model.Company, cancellationToken);

            if (model == null || existingInventory == null)
            {
                throw new Exception("Receiving Report or Inventory not found.");
            }

            var hasAlreadyBeenUsed = await _db.FilprideSalesInvoices
                .AnyAsync(si =>
                    si.ReceivingReportId == model.ReceivingReportId &&
                    si.Status != nameof(Status.Voided) &&
                    si.Status != nameof(Status.Canceled), cancellationToken);

            if (hasAlreadyBeenUsed)
            {
                throw new InvalidOperationException("This record has already been utilized in a sales invoice. Voiding is not permitted.");
            }

            model.PostedBy = null;
            model.VoidedBy = currentUser;
            model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
            model.Status = nameof(Status.Voided);

            await RemoveRecords<FilpridePurchaseBook>(pb => pb.DocumentNo == model.ReceivingReportNo, cancellationToken);
            await RemoveRecords<FilprideGeneralLedgerBook>(pb => pb.Reference == model.ReceivingReportNo, cancellationToken);
            var unitOfWork = new UnitOfWork(_db);
            await unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
            await RemoveQuantityReceived(model.POId, model.QuantityReceived, cancellationToken);

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(currentUser, $"Voided receiving report# {model.ReceivingReportNo}", "Receiving Report", model.Company);
            await _db.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task CreateEntriesForUpdatingCost(FilprideReceivingReport model, decimal difference, string userName, CancellationToken cancellationToken = default)
        {
            #region --General Ledger Recording

            var ledgers = new List<FilprideGeneralLedgerBook>();
            var isIncremental = difference > 0;
            difference = Math.Abs(difference);
            var particulars = $"Update Cost on DR#{model.DeliveryReceipt?.DeliveryReceiptNo}. DR dated {model.DeliveryReceipt?.DeliveredDate}";
            var netOfVatAmount = model.PurchaseOrder!.VatType == SD.VatType_Vatable
                ? ComputeNetOfVat(difference)
                : difference;
            var vatAmount = model.PurchaseOrder!.VatType == SD.VatType_Vatable
                ? ComputeVatAmount(netOfVatAmount)
                : 0m;
            var ewtAmount = model.PurchaseOrder!.TaxType == SD.TaxType_WithTax ? ComputeEwtAmount(netOfVatAmount, 0.01m) : 0m;

            if (model.PurchaseOrder.Terms == SD.Terms_Cod || model.PurchaseOrder.Terms == SD.Terms_Prepaid)
            {
                var advancesVoucher = await _db.FilprideCheckVoucherDetails
                    .Include(cv => cv.CheckVoucherHeader)
                    .FirstOrDefaultAsync(cv =>
                        cv.CheckVoucherHeader!.SupplierId == model.PurchaseOrder.SupplierId &&
                        cv.CheckVoucherHeader.IsAdvances &&
                        cv.CheckVoucherHeader.Status == nameof(CheckVoucherPaymentStatus.Posted) &&
                        cv.AccountName.Contains("Expanded Withholding Tax") &&
                        cv.Credit > cv.AmountPaid,
                        cancellationToken);

                if (advancesVoucher != null)
                {
                    var affectedEwt = Math.Min(advancesVoucher.Credit, ewtAmount);

                    if (isIncremental)
                    {
                        ewtAmount -= affectedEwt;
                        advancesVoucher.AmountPaid += affectedEwt;
                    }
                    else
                    {
                        ewtAmount += affectedEwt;
                        advancesVoucher.AmountPaid -= affectedEwt;
                    }
                }
            }

            var netOfEwtAmount = model.PurchaseOrder!.TaxType == SD.TaxType_WithTax
                ? ComputeNetOfEwt(difference, ewtAmount)
                : difference;

            var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(model.PurchaseOrder.Product!.ProductCode);
            var (cogsAcctNo, cogsAcctTitle) = GetCogsAccountTitle(model.PurchaseOrder.Product!.ProductCode);
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
            var ewtTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
            var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010100") ?? throw new ArgumentException("Account title '202010100' not found.");
            var inventoryTitle = accountTitlesDto.Find(c => c.AccountNumber == inventoryAcctNo) ?? throw new ArgumentException($"Account title '{inventoryAcctNo}' not found.");
            var cogsTitle = accountTitlesDto.Find(c => c.AccountNumber == cogsAcctNo) ?? throw new ArgumentException($"Account title '{cogsAcctNo}' not found.");

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                Reference = model.ReceivingReportNo!,
                Description = particulars,
                AccountId = inventoryTitle.AccountId,
                AccountNo = inventoryTitle.AccountNumber,
                AccountTitle = inventoryTitle.AccountName,
                Debit = isIncremental ? netOfVatAmount : 0,
                Credit = !isIncremental ? netOfVatAmount : 0,
                CreatedBy = userName,
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                Company = model.Company,
                ModuleType = nameof(ModuleType.Purchase)
            });

            if (vatAmount > 0)
            {
                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                    Reference = model.ReceivingReportNo!,
                    Description = particulars,
                    AccountId = vatInputTitle.AccountId,
                    AccountNo = vatInputTitle.AccountNumber,
                    AccountTitle = vatInputTitle.AccountName,
                    Debit = isIncremental ? vatAmount : 0,
                    Credit = !isIncremental ? vatAmount : 0,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    Company = model.Company,
                    ModuleType = nameof(ModuleType.Purchase)
                });
            }

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                Reference = model.ReceivingReportNo!,
                Description = particulars,
                AccountId = apTradeTitle.AccountId,
                AccountNo = apTradeTitle.AccountNumber,
                AccountTitle = apTradeTitle.AccountName,
                Debit = !isIncremental ? netOfEwtAmount : 0,
                Credit = isIncremental ? netOfEwtAmount : 0,
                CreatedBy = userName,
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                Company = model.Company,
                SupplierId = model.PurchaseOrder.SupplierId,
                SupplierName = model.PurchaseOrder.SupplierName,
                ModuleType = nameof(ModuleType.Purchase)
            });

            if (ewtAmount > 0)
            {
                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                    Reference = model.ReceivingReportNo!,
                    Description = particulars,
                    AccountId = ewtTitle.AccountId,
                    AccountNo = ewtTitle.AccountNumber,
                    AccountTitle = ewtTitle.AccountName,
                    Debit = !isIncremental ? ewtAmount : 0,
                    Credit = isIncremental ? ewtAmount : 0,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    Company = model.Company,
                    ModuleType = nameof(ModuleType.Purchase)
                });
            }

            if (model.DeliveryReceipt?.DeliveredDate != null)
            {
                var priceAdjustment = difference / model.QuantityReceived;
                var cogsAmount = model.DeliveryReceipt.Quantity *  priceAdjustment;
                var cogsNetOfVat = ComputeNetOfVat(cogsAmount);

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                    Reference = model.ReceivingReportNo!,
                    Description = particulars,
                    AccountId = cogsTitle.AccountId,
                    AccountNo = cogsTitle.AccountNumber,
                    AccountTitle = cogsTitle.AccountName,
                    Debit = isIncremental ? cogsNetOfVat : 0,
                    Credit = !isIncremental ? cogsNetOfVat : 0,
                    Company = model.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.Sales)
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                    Reference = model.ReceivingReportNo!,
                    Description = particulars,
                    AccountId = inventoryTitle.AccountId,
                    AccountNo = inventoryTitle.AccountNumber,
                    AccountTitle = inventoryTitle.AccountName,
                    Debit = !isIncremental ? cogsNetOfVat : 0,
                    Credit = isIncremental ? cogsNetOfVat : 0,
                    Company = model.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.Sales)
                });
            }

            if (!IsJournalEntriesBalanced(ledgers))
            {
                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
            }

            await _db.AddRangeAsync(ledgers, cancellationToken);

            #endregion --General Ledger Recording

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
