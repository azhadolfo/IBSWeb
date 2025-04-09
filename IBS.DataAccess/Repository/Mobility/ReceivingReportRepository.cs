using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Utility;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;

namespace IBS.DataAccess.Repository.Mobility
{
    public class ReceivingReportRepository : Repository<MobilityReceivingReport>, IReceivingReportRepository
    {
        private ApplicationDbContext _db;

        public ReceivingReportRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string stationCode, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(stationCode, cancellationToken);
            }
            else
            {
                return await GenerateCodeForUnDocumented(stationCode, cancellationToken);
            }
        }

        public async Task<string> GenerateCodeForDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            MobilityReceivingReport? lastRr = await _db
                .MobilityReceivingReports
                .Where(r => r.StationCode == stationCode && r.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr != null)
            {
                string lastSeries = lastRr.ReceivingReportNo;
                string numericPart = lastSeries.Substring(6);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return $"{lastSeries.Substring(0, 6) + incrementedNumber.ToString("D9")}";
            }
            else
            {
                return $"{stationCode}-RR000000001"; //S07-RR000000001
            }
        }

        private async Task<string> GenerateCodeForUnDocumented(string stationCode, CancellationToken cancellationToken)
        {
            MobilityReceivingReport? lastRr = await _db
                .MobilityReceivingReports
                .Where(r => r.StationCode == stationCode && r.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr != null)
            {
                string lastSeries = lastRr.ReceivingReportNo;
                string numericPart = lastSeries.Substring(7);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return $"{lastSeries.Substring(0, 7) + incrementedNumber.ToString("D8")}";
            }
            else
            {
                return $"{stationCode}-RRU00000001"; //S07-RRU00000001
            }
        }

        public override async Task<IEnumerable<MobilityReceivingReport>> GetAllAsync(Expression<Func<MobilityReceivingReport, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityReceivingReport> query = dbSet
                .Include(rr => rr.FilprideDeliveryReceipt)
                .ThenInclude(dr => dr.CustomerOrderSlip)
                .ThenInclude(cos => cos.PurchaseOrder)
                .ThenInclude(po => po.Product);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<MobilityReceivingReport> GetAsync(Expression<Func<MobilityReceivingReport, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(po => po.FilprideDeliveryReceipt)
                .ThenInclude(dr => dr.CustomerOrderSlip)
                .ThenInclude(cos => cos.PurchaseOrder)
                .ThenInclude(po => po.Product)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task PostAsync(MobilityReceivingReport receivingReport, CancellationToken cancellationToken = default)
        {
            //PENDING process the method here

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ReceivingReportViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingRecord = await _db.MobilityReceivingReports
                .FindAsync(viewModel.ReceivingReportId, cancellationToken);

            existingRecord.Date = viewModel.Date;
            existingRecord.Driver = viewModel.Driver;
            existingRecord.PlateNo = viewModel.PlateNo;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.DeliveryReceiptId = viewModel.DeliveryReceiptId;
            existingRecord.ReceivedQuantity = viewModel.ReceivedQuantity;

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = viewModel.CurrentUser;
                existingRecord.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<DateOnly> ComputeDueDateAsync(int poId, DateOnly rrDate, CancellationToken cancellationToken = default)
        {
            var po = await _db
                .MobilityPurchaseOrders
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

        public async Task<string> AutoGenerateReceivingReport(FilprideDeliveryReceipt deliveryReceipt, DateOnly deliveredDate, CancellationToken cancellationToken = default)
        {
            var getPurchasOrder = await _db.MobilityPurchaseOrders
                .Include(p => p.PickUpPoint)
                .FirstOrDefaultAsync(p => p.PurchaseOrderNo == deliveryReceipt.CustomerOrderSlip.CustomerPoNo, cancellationToken);
            MobilityReceivingReport model = new()
            {
                DeliveryReceiptId = 0,
                Date = deliveredDate,
                PurchaseOrderId = getPurchasOrder.PurchaseOrderId,
                PurchaseOrderNo = getPurchasOrder.PurchaseOrderNo,
                QuantityDelivered = deliveryReceipt.Quantity,
                QuantityReceived = deliveryReceipt.Quantity,
                TruckOrVessels = getPurchasOrder.PickUpPoint.Depot,
                AuthorityToLoadNo = deliveryReceipt.AuthorityToLoadNo,
                Driver = String.Empty,
                PlateNo = String.Empty,
                Remarks = "PENDING",
                Company = deliveryReceipt.Company,
                CreatedBy = "SYSTEM GENERATED",
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                PostedBy = "SYSTEM GENERATED",
                PostedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                Status = nameof(Status.Posted),
                Type = getPurchasOrder.Type,
                SupplierInvoiceDate = deliveredDate,
                StationCode = getPurchasOrder.StationCode
            };

            if (model.QuantityDelivered > getPurchasOrder.Quantity - getPurchasOrder.QuantityReceived)
            {
                throw new ArgumentException($"The inputted quantity exceeds the remaining delivered quantity for Purchase Order: " +
                                            $"{deliveryReceipt.PurchaseOrder.PurchaseOrderNo}. " +
                                            "Please contact the TNS department to verify the appointed supplier.");
            }

            var freight = deliveryReceipt.CustomerOrderSlip.DeliveryOption == SD.DeliveryOption_DirectDelivery
                ? deliveryReceipt.Freight
                : 0;

            model.ReceivedDate = model.Date;
            model.ReceivingReportNo = await GenerateCodeAsync(model.StationCode, model.Type, cancellationToken);
            model.DueDate = await ComputeDueDateAsync(model.PurchaseOrderId, model.Date, cancellationToken);
            model.GainOrLoss = model.QuantityDelivered - model.QuantityReceived;
            model.Amount = model.QuantityReceived * (getPurchasOrder.UnitPrice + freight);

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailCreate = new(model.PostedBy,
                $"Created new receiving report# {model.ReceivingReportNo}",
                "Receiving Report", "",
                model.Company);

            FilprideAuditTrail auditTrailPost = new(model.PostedBy,
                $"Posted receiving report# {model.ReceivingReportNo}",
                "Receiving Report", "",
                model.Company);

            await _db.AddAsync(auditTrailCreate, cancellationToken);
            await _db.AddAsync(auditTrailPost, cancellationToken);

            #endregion --Audit Trail Recording


            await _db.AddAsync(model, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            //await PostAsync(model, cancellationToken);

            return model.ReceivingReportNo;
        }
    }
}
