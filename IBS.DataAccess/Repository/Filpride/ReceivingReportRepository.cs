using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Utility;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ReceivingReportRepository : Repository<FilprideReceivingReport>, IReceivingReportRepository
    {
        private ApplicationDbContext _db;

        public ReceivingReportRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<DateOnly> ComputeDueDateAsync(int poId, DateOnly rrDate, CancellationToken cancellationToken = default)
        {
            var po = await _db
                .FilpridePurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == poId, cancellationToken);

            if (po != null)
            {
                DateOnly dueDate;

                switch (po.Terms)
                {
                    case "7D":
                        return dueDate = rrDate.AddDays(7);

                    case "10D":
                        return dueDate = rrDate.AddDays(7);

                    case "15D":
                        return dueDate = rrDate.AddDays(15);

                    case "30D":
                        return dueDate = rrDate.AddDays(30);

                    case "45D":
                    case "45PDC":
                        return dueDate = rrDate.AddDays(45);

                    case "60D":
                    case "60PDC":
                        return dueDate = rrDate.AddDays(60);

                    case "90D":
                        return dueDate = rrDate.AddDays(90);

                    case "M15":
                        return dueDate = rrDate.AddMonths(1).AddDays(15 - rrDate.Day);

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

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-2);
                            }
                            else if (dueDate.Day == 30)
                            {
                                dueDate = dueDate.AddDays(-1);
                            }
                        }
                        return dueDate;

                    default:
                        return dueDate = rrDate;
                }
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public async Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(company, cancellationToken);
            }
            else
            {
                return await GenerateCodeForUnDocumented(company, cancellationToken);
            }
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideReceivingReport? lastRr = await _db
                .FilprideReceivingReports
                .Where(rr => rr.Company == company && !rr.ReceivingReportNo.StartsWith("RRBEG") && rr.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr != null)
            {
                string lastSeries = lastRr.ReceivingReportNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "RR0000000001";
            }
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideReceivingReport? lastRr = await _db
                .FilprideReceivingReports
                .Where(rr => rr.Company == company && !rr.ReceivingReportNo.StartsWith("RRBEG") && rr.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr != null)
            {
                string lastSeries = lastRr.ReceivingReportNo;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
            }
            else
            {
                return "RRU000000001";
            }
        }

        public async Task<List<SelectListItem>> GetReceivingReportListAsync(string[] rrNos, string company, CancellationToken cancellationToken = default)
        {
            var rrNoHashSet = new HashSet<string>(rrNos);

            var rrList = await _db.FilprideReceivingReports
                .OrderBy(rr => rrNoHashSet.Contains(rr.ReceivingReportNo) ? Array.IndexOf(rrNos, rr.ReceivingReportNo) : int.MaxValue) // Order by index in rrNos array if present in HashSet
                .ThenBy(rr => rr.ReceivingReportId) // Secondary ordering by Id
                .Where(rr => rr.Company == company)
                .Select(rr => new SelectListItem
                {
                    Value = rr.ReceivingReportNo,
                    Text = rr.ReceivingReportNo
                })
                .ToListAsync(cancellationToken);

            return rrList;
        }

        public async Task<int> RemoveQuantityReceived(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _db.FilpridePurchaseOrders
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (po != null)
            {
                po.QuantityReceived -= quantityReceived;

                if (po.IsReceived)
                {
                    po.IsReceived = false;
                    po.ReceivedDate = DateTime.MaxValue;
                }
                if (po.QuantityReceived > po.Quantity)
                {
                    throw new ArgumentException("Input is exceed to remaining quantity received");
                }

                return await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public async Task UpdatePOAsync(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _db.FilpridePurchaseOrders
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (po != null)
            {
                po.QuantityReceived += quantityReceived;

                if (po.QuantityReceived == po.Quantity)
                {
                    po.IsReceived = true;
                    po.ReceivedDate = DateTime.Now;

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

        public override async Task<FilprideReceivingReport> GetAsync(Expression<Func<FilprideReceivingReport, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr.Customer)
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideReceivingReport>> GetAllAsync(Expression<Func<FilprideReceivingReport, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideReceivingReport> query = dbSet
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr.Customer)
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task AutoGenerateReceivingReport(FilprideDeliveryReceipt deliveryReceipt, CancellationToken cancellationToken = default)
        {
            FilprideReceivingReport model = new()
            {
                DeliveryReceiptId = deliveryReceipt.DeliveryReceiptId,
                Date = (DateOnly)deliveryReceipt.DeliveredDate,
                POId = deliveryReceipt.PurchaseOrder.PurchaseOrderId,
                PONo = deliveryReceipt.PurchaseOrder.PurchaseOrderNo,
                QuantityDelivered = deliveryReceipt.Quantity,
                QuantityReceived = deliveryReceipt.Quantity,
                TruckOrVessels = deliveryReceipt.CustomerOrderSlip.PickUpPoint.Depot,
                AuthorityToLoadNo = deliveryReceipt.AuthorityToLoadNo,
                Remarks = "PENDING",
                Company = deliveryReceipt.Company,
                CreatedBy = "SYSTEM GENERATED",
                CreatedDate = DateTime.Now,
                PostedBy = "SYSTEM GENERATED",
                PostedDate = DateTime.Now,
                Status = nameof(Status.Posted),
                Type = deliveryReceipt.PurchaseOrder.Type
            };

            if (model.QuantityDelivered > deliveryReceipt.PurchaseOrder.Quantity - deliveryReceipt.PurchaseOrder.QuantityReceived)
            {
                throw new ArgumentException("Inputted quantity is exceed to remaining quantity delivered");
            }

            if (deliveryReceipt.CustomerOrderSlip.HasMultiplePO)
            {
                var appointedSupplier = await _db.FilprideCOSAppointedSuppliers
                    .OrderBy(a => a.SequenceId)
                    .FirstOrDefaultAsync(a => a.CustomerOrderSlipId == deliveryReceipt.CustomerOrderSlipId && a.PurchaseOrderId == deliveryReceipt.PurchaseOrderId);

                appointedSupplier.UnservedQuantity -= deliveryReceipt.Quantity;
            }

            model.ReceivedDate = model.Date;
            model.ReceivingReportNo = await GenerateCodeAsync(model.Company, model.Type, cancellationToken);
            model.DueDate = await ComputeDueDateAsync(model.POId, model.Date, cancellationToken);
            model.GainOrLoss = model.QuantityDelivered - model.QuantityReceived;
            model.Amount = model.QuantityReceived * deliveryReceipt.PurchaseOrder.Price;

            await _db.AddAsync(model, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await PostAsync(model, cancellationToken);

        }

        public async Task PostAsync(FilprideReceivingReport model, CancellationToken cancellationToken = default)
        {
            #region --General Ledger Recording

            var ledgers = new List<FilprideGeneralLedgerBook>();

            decimal netOfVatAmount = 0;
            decimal vatAmount = 0;
            decimal ewtAmount = 0;
            decimal netOfEwtAmount = 0;

            if (model.PurchaseOrder.Supplier.VatType == SD.VatType_Vatable)
            {
                netOfVatAmount = ComputeNetOfVat(model.Amount);
                vatAmount = ComputeVatAmount(netOfVatAmount);
            }
            else
            {
                netOfVatAmount = model.Amount;
            }

            if (model.PurchaseOrder.Supplier.TaxType == SD.TaxType_WithTax)
            {
                ewtAmount = ComputeEwtAmount(netOfVatAmount, 0.01m);
                netOfEwtAmount = ComputeNetOfEwt(model.Amount, ewtAmount);
            }
            else
            {
                netOfEwtAmount = model.Amount;
            }

            var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(model.PurchaseOrder.Product.ProductCode);
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "1010602") ?? throw new ArgumentException("Account title '1010602' not found.");
            var ewtTitle = accountTitlesDto.Find(c => c.AccountNumber == "2010302") ?? throw new ArgumentException("Account title '2010302' not found.");
            var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "2010101") ?? throw new ArgumentException("Account title '2010101' not found.");

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = model.Date,
                Reference = model.ReceivingReportNo,
                Description = "Receipt of Goods",
                AccountNo = inventoryAcctNo,
                AccountTitle = inventoryAcctTitle,
                Debit = netOfVatAmount,
                Credit = 0,
                CreatedBy = model.CreatedBy,
                CreatedDate = model.CreatedDate,
                Company = model.Company
            });

            if (vatAmount > 0)
            {
                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = model.Date,
                    Reference = model.ReceivingReportNo,
                    Description = "Receipt of Goods",
                    AccountNo = vatInputTitle.AccountNumber,
                    AccountTitle = vatInputTitle.AccountName,
                    Debit = vatAmount,
                    Credit = 0,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = model.CreatedDate,
                    Company = model.Company
                });
            }

            if (ewtAmount > 0)
            {
                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = model.Date,
                    Reference = model.ReceivingReportNo,
                    Description = "Receipt of Goods",
                    AccountNo = ewtTitle.AccountNumber,
                    AccountTitle = ewtTitle.AccountName,
                    Debit = 0,
                    Credit = ewtAmount,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = model.CreatedDate,
                    Company = model.Company
                });
            }

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = model.Date,
                Reference = model.ReceivingReportNo,
                Description = "Receipt of Goods",
                AccountNo = apTradeTitle.AccountNumber,
                AccountTitle = apTradeTitle.AccountName,
                Debit = 0,
                Credit = netOfEwtAmount,
                CreatedBy = model.CreatedBy,
                CreatedDate = model.CreatedDate,
                Company = model.Company
            });

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

            await UpdatePOAsync(model.PurchaseOrder.PurchaseOrderId, model.QuantityReceived, cancellationToken);

            #region --Purchase Book Recording

            FilpridePurchaseBook purchaseBook = new()
            {
                Date = model.Date,
                SupplierName = model.PurchaseOrder.Supplier.SupplierName,
                SupplierTin = model.PurchaseOrder.Supplier.SupplierTin,
                SupplierAddress = model.PurchaseOrder.Supplier.SupplierAddress,
                DocumentNo = model.ReceivingReportNo,
                Description = model.PurchaseOrder.Product.ProductName,
                Amount = model.Amount,
                VatAmount = vatAmount,
                WhtAmount = ewtAmount,
                NetPurchases = netOfVatAmount,
                CreatedBy = model.CreatedBy,
                PONo = model.PurchaseOrder.PurchaseOrderNo,
                DueDate = model.DueDate,
                Company = model.Company
            };

            await _db.AddAsync(purchaseBook, cancellationToken);
            #endregion --Purchase Book Recording

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}