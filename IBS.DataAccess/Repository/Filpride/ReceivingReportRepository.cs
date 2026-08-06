using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DTOs;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
            return type switch
            {
                nameof(DocumentType.Documented) => await GenerateCodeForDocumented(company, cancellationToken),
                nameof(DocumentType.Undocumented) => await GenerateCodeForUnDocumented(company, cancellationToken),
                _ => throw new ArgumentException("Invalid type")
            };
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastRr = await _db
                .FilprideReceivingReports
                .AsNoTracking()
                .OrderByDescending(x => x.ReceivingReportNo!.Length)
                .ThenByDescending(x => x.ReceivingReportNo)
                .FirstOrDefaultAsync(x =>
                    x.Company == company &&
                    x.Type == nameof(DocumentType.Documented) &&
                    !x.ReceivingReportNo!.Contains("RRBEG"),
                    cancellationToken);

            if (lastRr == null)
            {
                return "RR0000000001";
            }

            var lastSeries = lastRr.ReceivingReportNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastRr = await _db
                .FilprideReceivingReports
                .AsNoTracking()
                .OrderByDescending(x => x.ReceivingReportNo!.Length)
                .ThenByDescending(x => x.ReceivingReportNo)
                .FirstOrDefaultAsync(x =>
                        x.Company == company &&
                        x.Type == nameof(DocumentType.Undocumented) &&
                        !x.ReceivingReportNo!.Contains("RRBEG"),
                    cancellationToken);

            if (lastRr == null)
            {
                return "RRU000000001";
            }

            var lastSeries = lastRr.ReceivingReportNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = long.Parse(numericPart) + 1;

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

            po.ActualPrices.FirstOrDefault()!.AppliedVolume -= quantityReceived;
            return await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdatePoAsync(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _db.FilpridePurchaseOrders
                         .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken)
                     ?? throw new ArgumentException("No record found.");

            var updatedQty = po.QuantityReceived + quantityReceived;
            if (updatedQty > po.Quantity)
            {
                throw new ArgumentException("Input is exceed to remaining quantity received");
            }

            po.QuantityReceived = updatedQty;
            po.IsReceived = po.QuantityReceived == po.Quantity;
            if (po.IsReceived)
            {
                po.ReceivedDate = DateTimeHelper.GetCurrentPhilippineTime();
            }

            await _db.SaveChangesAsync(cancellationToken);
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

        public override IQueryable<FilprideReceivingReport> GetAllQuery(Expression<Func<FilprideReceivingReport, bool>>? filter = null)
        {
            IQueryable<FilprideReceivingReport> query = dbSet
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr!.Customer)
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .AsSplitQuery()
                .AsNoTracking();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return query;
        }

        public async Task<string> AutoGenerateReceivingReport(FilprideDeliveryReceipt deliveryReceipt,
            DateOnly liftingDate,
            string userName,
            CancellationToken cancellationToken = default)
        {
            var effectiveDetails = deliveryReceipt.Details.Any()
                ? deliveryReceipt.Details
                : throw new ArgumentException("Delivery receipt details are required to generate receiving reports.");

            var detailGroups = effectiveDetails
                .GroupBy(d => d.PurchaseOrderId)
                .ToList();

            var generatedReceivingReportNos = new List<string>();

            foreach (var detailGroup in detailGroups)
            {
                var firstDetail = detailGroup.First();
                var purchaseOrder = firstDetail.PurchaseOrder
                    ?? throw new ArgumentException($"Purchase order {firstDetail.PurchaseOrderId} not found for receiving report generation.");
                var customerOrderSlip = firstDetail.CustomerOrderSlip ?? deliveryReceipt.CustomerOrderSlip
                    ?? throw new ArgumentException("Customer order slip not found for receiving report generation.");
                var groupedQuantity = detailGroup.Sum(d => d.Quantity);
                var distinctAtlNos = detailGroup
                    .Select(d => d.AuthorityToLoadNo)
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Distinct()
                    .ToList();

                FilprideReceivingReport model = new()
                {
                    DeliveryReceiptId = deliveryReceipt.DeliveryReceiptId,
                    Date = liftingDate,
                    POId = purchaseOrder.PurchaseOrderId,
                    PONo = purchaseOrder.PurchaseOrderNo,
                    QuantityDelivered = groupedQuantity,
                    QuantityReceived = groupedQuantity,
                    TruckOrVessels = customerOrderSlip.PickUpPoint!.Depot,
                    AuthorityToLoadNo = distinctAtlNos.Count == 1 ? distinctAtlNos[0] : deliveryReceipt.AuthorityToLoadNo,
                    Remarks = "PENDING",
                    Company = deliveryReceipt.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    PostedBy = userName,
                    PostedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    Status = nameof(Status.Posted),
                    Type = purchaseOrder.Type,
                    TaxPercentage = purchaseOrder.Supplier!.WithholdingTaxPercent ?? 0m
                };

                if (model.QuantityDelivered > purchaseOrder.Quantity - purchaseOrder.QuantityReceived)
                {
                    throw new ArgumentException($"The inputted quantity exceeds the remaining balance for Purchase Order: {purchaseOrder.PurchaseOrderNo}.");
                }

                var freight = customerOrderSlip.DeliveryOption == SD.DeliveryOption_DirectDelivery
                    ? (decimal)customerOrderSlip.Freight!
                    : 0m;

                model.ReceivedDate = model.Date;
                model.ReceivingReportNo = await GenerateCodeAsync(model.Company, model.Type!, cancellationToken);
                model.DueDate = await ComputeDueDateAsync(purchaseOrder.Terms, model.Date, cancellationToken);
                model.GainOrLoss = model.QuantityDelivered - model.QuantityReceived;

                var poActualPrice = await _db.FilpridePOActualPrices
                    .FirstOrDefaultAsync(a => a.PurchaseOrderId == purchaseOrder.PurchaseOrderId
                                              && a.IsApproved
                                              && a.AppliedVolume != a.TriggeredVolume,
                        cancellationToken);

                var remainingQuantity = model.QuantityReceived;
                decimal totalAmount = 0m;

                if (poActualPrice != null)
                {
                    var availableQuantity = poActualPrice.TriggeredVolume - poActualPrice.AppliedVolume;

                    if (availableQuantity > 0)
                    {
                        var applicableQuantity = Math.Min(remainingQuantity, availableQuantity);
                        var applicableUnitCost = RoundToFourDecimalPlaces(poActualPrice.TriggeredPrice + freight);
                        totalAmount += applicableQuantity * applicableUnitCost;
                        poActualPrice.AppliedVolume += applicableQuantity;
                        remainingQuantity -= applicableQuantity;
                    }
                }

                var remainingUnitCost = RoundToFourDecimalPlaces((poActualPrice?.TriggeredPrice ?? purchaseOrder.Price) + freight);
                totalAmount += remainingQuantity * remainingUnitCost;
                model.Amount = totalAmount;

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
                await _db.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                await PostAsync(model, cancellationToken);
                var unitOfWork = new UnitOfWork(_db);
                await unitOfWork.FilprideInventory.AddPurchaseToInventoryAsync(model, cancellationToken);
                await UpdatePoAsync(model.PurchaseOrder!.PurchaseOrderId, model.QuantityReceived, cancellationToken);

                generatedReceivingReportNos.Add(model.ReceivingReportNo);
            }

            var salesInvoice = await _db.FilprideSalesInvoices
                .FirstOrDefaultAsync(si => si.DeliveryReceiptId == deliveryReceipt.DeliveryReceiptId, cancellationToken);

            if (salesInvoice != null)
            {
                if (generatedReceivingReportNos.Count == 1)
                {
                    var rrId = await _db.FilprideReceivingReports
                        .Where(rr => rr.DeliveryReceiptId == deliveryReceipt.DeliveryReceiptId
                                     && rr.ReceivingReportNo == generatedReceivingReportNos[0])
                        .Select(rr => rr.ReceivingReportId)
                        .FirstAsync(cancellationToken);

                    salesInvoice.ReceivingReportId = rrId;
                }
                else
                {
                    salesInvoice.ReceivingReportId = 0;
                }
            }

            return string.Join(", ", generatedReceivingReportNos);
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

            if (model.PurchaseOrder.Terms == SD.Terms_Cod || model.PurchaseOrder.Terms == SD.Terms_Prepaid)
            {
                ewtAmount = await ApplyAdvanceEwtOffsetAsync(model, ewtAmount, isReversal: false, cancellationToken);
            }

            var netOfEwtAmount = model.PurchaseOrder!.TaxType == SD.TaxType_WithTax
                ? ComputeNetOfEwt(model.Amount, ewtAmount)
                : model.Amount;

            var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(model.PurchaseOrder.Product!.ProductCode);
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200")
                                ?? throw new ArgumentException("Account title '101060200' not found.");
            AccountTitleDto? ewtTitle = null;
            if (ewtAmount > 0)
            {
                var ewtAccountNo = WithholdingTaxHelper.GetAccountNumberByPercent(model.TaxPercentage)
                    ?? throw new ArgumentException($"No EWT account mapping found for tax percentage '{model.TaxPercentage}'.");
                ewtTitle = accountTitlesDto.FirstOrDefault(c => c.AccountNumber == ewtAccountNo)
                    ?? throw new ArgumentException($"Account title '{ewtAccountNo}' not found.");
            }
            var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010100")
                               ?? throw new ArgumentException("Account title '201010100' not found.");
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
                CreatedBy = model.PostedBy!,
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
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
                    CreatedBy = model.PostedBy!,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
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
                CreatedBy = model.PostedBy!,
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                Company = model.Company,
                SubAccountType = SubAccountType.Supplier,
                SubAccountId = model.PurchaseOrder.SupplierId,
                SubAccountName = model.PurchaseOrder.SupplierName,
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
                    CreatedBy = model.PostedBy!,
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

            var existingSalesInvoice = await _db.FilprideSalesInvoices
                .FirstOrDefaultAsync(si =>
                    si.ReceivingReportId == model.ReceivingReportId &&
                    si.Status != nameof(Status.Voided) &&
                    si.Status != nameof(Status.Canceled), cancellationToken);

            existingSalesInvoice?.ReceivingReportId = 0;

            model.VoidedBy = currentUser;
            model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
            model.Status = nameof(Status.Voided);
            model.PostedBy = null;

            if (model.PurchaseOrder != null &&
                (model.PurchaseOrder.Terms == SD.Terms_Cod || model.PurchaseOrder.Terms == SD.Terms_Prepaid))
            {
                var netOfVatAmount = model.PurchaseOrder.VatType == SD.VatType_Vatable
                    ? ComputeNetOfVat(model.Amount)
                    : model.Amount;
                var ewtAmount = model.PurchaseOrder.TaxType == SD.TaxType_WithTax
                    ? ComputeEwtAmount(netOfVatAmount, model.TaxPercentage)
                    : 0m;

                await ApplyAdvanceEwtOffsetAsync(model, ewtAmount, isReversal: true, cancellationToken);
            }

            var unitOfWork = new UnitOfWork(_db);
            await unitOfWork.GeneralLedger.ReverseEntries(model.ReceivingReportNo, cancellationToken);

            await unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
            await RemoveQuantityReceived(model.POId, model.QuantityReceived, cancellationToken);

            var hasActiveReceivingReports = await _db.FilprideReceivingReports
                .AnyAsync(rr => rr.DeliveryReceiptId == model.DeliveryReceiptId
                                && rr.ReceivingReportId != model.ReceivingReportId
                                && rr.Status != nameof(Status.Voided)
                                && rr.Status != nameof(Status.Canceled), cancellationToken);

            if (model.DeliveryReceipt != null)
            {
                model.DeliveryReceipt.HasReceivingReport = hasActiveReceivingReports;
            }

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(currentUser, $"Voided receiving report# {model.ReceivingReportNo}", "Receiving Report", model.Company);
            await _db.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<decimal> ApplyAdvanceEwtOffsetAsync(
            FilprideReceivingReport model,
            decimal ewtAmount,
            bool isReversal,
            CancellationToken cancellationToken)
        {
            if (ewtAmount <= 0 || model.PurchaseOrder?.SupplierId == null)
            {
                return ewtAmount;
            }

            var advancesVouchers = await _db.FilprideCheckVoucherDetails
                .Include(cv => cv.CheckVoucherHeader)
                .Where(cv =>
                    cv.CheckVoucherHeader!.SupplierId == model.PurchaseOrder.SupplierId &&
                    cv.CheckVoucherHeader.IsAdvances &&
                    cv.CheckVoucherHeader.Status == nameof(CheckVoucherPaymentStatus.Posted) &&
                    cv.AccountName.Contains("Expanded Withholding Tax") &&
                    (isReversal ? cv.AmountPaid > 0 : cv.Credit > cv.AmountPaid))
                .OrderBy(cv => cv.CheckVoucherHeader!.Date)
                .ThenBy(cv => cv.CheckVoucherHeaderId)
                .ThenBy(cv => cv.CheckVoucherDetailId)
                .ToListAsync(cancellationToken);

            if (advancesVouchers.Count == 0)
            {
                return ewtAmount;
            }

            var remainingEwt = ewtAmount;

            if (remainingEwt <= 0)
            {
                return ewtAmount;
            }

            foreach (var advancesVoucher in advancesVouchers)
            {
                if (remainingEwt <= 0)
                {
                    break;
                }

                var availableAmount = isReversal
                    ? advancesVoucher.AmountPaid
                    : advancesVoucher.Credit - advancesVoucher.AmountPaid;

                if (availableAmount <= 0)
                {
                    continue;
                }

                var affectedEwt = Math.Min(availableAmount, remainingEwt);
                advancesVoucher.AmountPaid += isReversal ? -affectedEwt : affectedEwt;
                remainingEwt -= affectedEwt;
            }

            return isReversal ? ewtAmount : remainingEwt;
        }

        public async Task CreateEntriesForUpdatingCost(FilprideReceivingReport model, decimal difference, string userName, CancellationToken cancellationToken = default)
        {
            #region --General Ledger Recording

            var ledgers = new List<FilprideGeneralLedgerBook>();
            var isIncremental = difference > 0;
            difference = Math.Abs(difference);
            var unitOfWork = new UnitOfWork(_db);
            var firstDayOfMonth = DateTimeHelper.GetFirstDayOfCurrentPhilippineMonth();
            var receivingReportDate = model.Date;
            var isReceivingReportPeriodPosted = await unitOfWork
                .IsPeriodPostedAsync(Module.ReceivingReport, receivingReportDate, cancellationToken);
            var purchasePostingDate = isReceivingReportPeriodPosted
                ? firstDayOfMonth
                : receivingReportDate;
            var particulars = $"Update Cost on DR#{model.DeliveryReceipt!.DeliveryReceiptNo}. " +
                              $"DR dated {model.DeliveryReceipt!.DeliveredDate}";
            var netOfVatAmount = model.PurchaseOrder!.VatType == SD.VatType_Vatable
                ? ComputeNetOfVat(difference)
                : difference;
            var vatAmount = model.PurchaseOrder!.VatType == SD.VatType_Vatable
                ? ComputeVatAmount(netOfVatAmount)
                : 0m;
            var ewtAmount = model.PurchaseOrder!.TaxType == SD.TaxType_WithTax
                ? ComputeEwtAmount(netOfVatAmount, model.TaxPercentage)
                : 0m;


            if (model.PurchaseOrder.Terms == SD.Terms_Cod || model.PurchaseOrder.Terms == SD.Terms_Prepaid)
            {
                if (isIncremental)
                {
                    ewtAmount = await ApplyAdvanceEwtOffsetAsync(model, ewtAmount, isReversal: false, cancellationToken);
                }
                else
                {
                    await ApplyAdvanceEwtOffsetAsync(model, ewtAmount, isReversal: true, cancellationToken);
                }
            }

            var netOfEwtAmount = model.PurchaseOrder!.TaxType == SD.TaxType_WithTax
                ? ComputeNetOfEwt(difference, ewtAmount)
                : difference;

            var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(model.PurchaseOrder.Product!.ProductCode);
            var (cogsAcctNo, cogsAcctTitle) = GetCogsAccountTitle(model.PurchaseOrder.Product!.ProductCode);
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
            AccountTitleDto? ewtTitle = null;
            if (ewtAmount > 0)
            {
                var ewtAccountNo = WithholdingTaxHelper.GetAccountNumberByPercent(model.TaxPercentage)
                    ?? throw new ArgumentException($"No EWT account mapping found for tax percentage '{model.TaxPercentage}'.");
                ewtTitle = accountTitlesDto.FirstOrDefault(c => c.AccountNumber == ewtAccountNo)
                               ?? throw new ArgumentException($"Account title '{ewtAccountNo}' not found.");
            }
            var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010100") ?? throw new ArgumentException("Account title '201010100' not found.");
            var inventoryTitle = accountTitlesDto.Find(c => c.AccountNumber == inventoryAcctNo) ?? throw new ArgumentException($"Account title '{inventoryAcctNo}' not found.");
            var cogsTitle = accountTitlesDto.Find(c => c.AccountNumber == cogsAcctNo) ?? throw new ArgumentException($"Account title '{cogsAcctNo}' not found.");

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = purchasePostingDate,
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
                    Date = purchasePostingDate,
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
                Date = purchasePostingDate,
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
                SubAccountType = SubAccountType.Supplier,
                SubAccountId = model.PurchaseOrder.SupplierId,
                SubAccountName = model.PurchaseOrder.SupplierName,
                ModuleType = nameof(ModuleType.Purchase)
            });

            if (ewtAmount > 0)
            {
                ledgers.Add(new FilprideGeneralLedgerBook
                {
                        Date = purchasePostingDate,
                        Reference = model.ReceivingReportNo!,
                        Description = particulars,
                        AccountId = ewtTitle!.AccountId,
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
                var deliveredDate = model.DeliveryReceipt.DeliveredDate.Value;
                var isDeliveredPeriodPosted = await unitOfWork
                    .IsPeriodPostedAsync(Module.DeliveryReceipt, deliveredDate, cancellationToken);
                var cogsPostingDate = isDeliveredPeriodPosted
                    ? firstDayOfMonth
                    : deliveredDate;

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = cogsPostingDate,
                    Reference = model.DeliveryReceipt.DeliveryReceiptNo,
                    Description = particulars,
                    AccountId = cogsTitle.AccountId,
                    AccountNo = cogsTitle.AccountNumber,
                    AccountTitle = cogsTitle.AccountName,
                    Debit = isIncremental ? netOfVatAmount : 0,
                    Credit = !isIncremental ? netOfVatAmount : 0,
                    Company = model.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.Sales)
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = cogsPostingDate,
                    Reference = model.DeliveryReceipt.DeliveryReceiptNo,
                    Description = particulars,
                    AccountId = inventoryTitle.AccountId,
                    AccountNo = inventoryTitle.AccountNumber,
                    AccountTitle = inventoryTitle.AccountName,
                    Debit = !isIncremental ? netOfVatAmount : 0,
                    Credit = isIncremental ? netOfVatAmount : 0,
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
