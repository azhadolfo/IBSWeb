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
        private IPurchaseOrderRepository FilpridePurchaseOrder { get; set; }

        public ReceivingReportRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
            FilpridePurchaseOrder = new PurchaseOrderRepository(_db);
        }

        public async Task<DateOnly> ComputeDueDateAsync(int poId, DateOnly rrDate, CancellationToken cancellationToken = default)
        {
            var po = await _db
                .FilpridePurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == poId, cancellationToken);

            if (po == null)
            {
                throw new ArgumentException("No record found.");
            }

            DateOnly dueDate;

            switch (po.Terms)
            {
                case "7D":
                case "10D":
                    return rrDate.AddDays(7);

                case "15D":
                    return rrDate.AddDays(15);

                case "30D":
                    return rrDate.AddDays(30);

                case "45D":
                case "45PDC":
                    return rrDate.AddDays(45);

                case "60D":
                case "60PDC":
                    return rrDate.AddDays(60);

                case "90D":
                    return rrDate.AddDays(90);

                case "M15":
                    return rrDate.AddMonths(1).AddDays(15 - rrDate.Day);

                case "M30":
                    if (rrDate.Month == 1)
                    {
                        dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);
                    }
                    else
                    {
                        dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);

                        if (dueDate.Day == 31)
                        {
                            dueDate = dueDate.AddDays(-1);
                        }
                    }
                    return dueDate;

                case "M29":
                    if (rrDate.Month == 1)
                    {
                        dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);
                    }
                    else
                    {
                        dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);

                        switch (dueDate.Day)
                        {
                            case 31:
                                dueDate = dueDate.AddDays(-2);
                                break;
                            case 30:
                                dueDate = dueDate.AddDays(-1);
                                break;
                        }
                    }
                    return dueDate;

                default:
                    return rrDate;
            }
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
            model.DueDate = await ComputeDueDateAsync(model.POId, model.Date, cancellationToken);
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
            totalAmount += remainingQuantity * (await FilpridePurchaseOrder.GetPurchaseOrderCost(deliveryReceipt.PurchaseOrder.PurchaseOrderId, cancellationToken) + freight);
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
                    ewtAmount -= affectedEwt;
                    advancesVoucher.AmountPaid += affectedEwt;
                }
            }

            var netOfEwtAmount = model.PurchaseOrder!.TaxType == SD.TaxType_WithTax ? ComputeNetOfEwt(model.Amount, ewtAmount) : model.Amount;

            var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(model.PurchaseOrder.Product!.ProductCode);
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
            var ewtTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
            var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010100") ?? throw new ArgumentException("Account title '202010100' not found.");
            var inventoryTitle = accountTitlesDto.Find(c => c.AccountNumber == inventoryAcctNo) ?? throw new ArgumentException($"Account title '{inventoryAcctNo}' not found.");

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
                Company = model.Company
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
                    Company = model.Company
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
                SupplierName = model.PurchaseOrder.SupplierName
            });

            if (ewtAmount > 0)
            {
                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = model.Date,
                    Reference = model.ReceivingReportNo!,
                    Description = "Receipt of Goods",
                    AccountId = ewtTitle.AccountId,
                    AccountNo = ewtTitle.AccountNumber,
                    AccountTitle = ewtTitle.AccountName,
                    Debit = 0,
                    Credit = ewtAmount,
                    CreatedBy = model.PostedBy,
                    CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    Company = model.Company
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
    }
}
