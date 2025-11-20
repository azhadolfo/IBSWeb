using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Status = IBS.Utility.Enums.Status;

namespace IBS.DataAccess.Repository.Mobility
{
    public class ReceivingReportRepository : Repository<MobilityReceivingReport>, IReceivingReportRepository
    {
        private readonly ApplicationDbContext _db;

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

            return await GenerateCodeForUnDocumented(stationCode, cancellationToken);
        }

        private async Task<string> GenerateCodeForDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            MobilityReceivingReport? lastRr = await _db
                .MobilityReceivingReports
                .Where(r => r.StationCode == stationCode && r.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr == null)
            {
                return $"{stationCode}-RR000000001";
            }

            var lastSeries = lastRr.ReceivingReportNo;
            var numericPart = lastSeries.Substring(6);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return $"{lastSeries.Substring(0, 6) + incrementedNumber.ToString("D9")}";

        }

        private async Task<string> GenerateCodeForUnDocumented(string stationCode, CancellationToken cancellationToken)
        {
            var lastRr = await _db
                .MobilityReceivingReports
                .Where(r => r.StationCode == stationCode && r.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr == null)
            {
                return $"{stationCode}-RRU00000001"; //S07-RRU00000001
            }

            var lastSeries = lastRr.ReceivingReportNo;
            var numericPart = lastSeries.Substring(7);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return $"{lastSeries.Substring(0, 7) + incrementedNumber.ToString("D8")}";
        }

        public override async Task<IEnumerable<MobilityReceivingReport>> GetAllAsync(Expression<Func<MobilityReceivingReport, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityReceivingReport> query = dbSet
                .Include(rr => rr.PurchaseOrder)
                .ThenInclude(dr => dr!.Product);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<MobilityReceivingReport?> GetAsync(Expression<Func<MobilityReceivingReport, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(po => po.PurchaseOrder)
                .ThenInclude(po => po!.Product)
                .Include(po => po.PurchaseOrder)
                .ThenInclude(po => po!.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task PostAsync(MobilityReceivingReport receivingReport, CancellationToken cancellationToken = default)
        {
            await UpdatePoAsync(receivingReport.PurchaseOrder!.PurchaseOrderId, receivingReport.QuantityReceived, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ReceivingReportViewModel viewModel, string stationCodeClaim, CancellationToken cancellationToken)
        {
            var existingRecord = await _db.MobilityReceivingReports
                .FirstOrDefaultAsync(x => x.ReceivingReportId == viewModel.ReceivingReportId, cancellationToken)
                                 ?? throw new NullReferenceException("Receiving report not found");

            #region --Retrieve PO

            var existingPo = await _db.MobilityPurchaseOrders
                .Include(po => po.Product)
                .Include(po => po.Supplier)
                .Include(po => po.PickUpPoint)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken);

            #endregion --Retrieve PO

            var totalAmountRr = existingPo!.Quantity - existingPo.QuantityReceived;

            if (viewModel.QuantityDelivered > totalAmountRr)
            {
                viewModel.DrList = await _db.FilprideDeliveryReceipts
                    .OrderBy(dr => dr.DeliveryReceiptId)
                    .Where(dr => dr.DeliveredDate != null)
                    .Select(dr => new SelectListItem
                    {
                        Value = dr.DeliveryReceiptId.ToString(),
                        Text = dr.DeliveryReceiptNo
                    })
                    .ToListAsync(cancellationToken);
                viewModel.PurchaseOrders = await _db.MobilityPurchaseOrders
                    .Where(po =>
                        po.StationCode == stationCodeClaim && !po.IsReceived && po.PostedBy != null && !po.IsClosed)
                    .Select(po => new SelectListItem
                    {
                        Value = po.PurchaseOrderId.ToString(),
                        Text = po.PurchaseOrderNo
                    })
                    .ToListAsync(cancellationToken);

                throw new InvalidOperationException("Input is exceed to remaining quantity delivered");
            }

            var deliveryReceipt = await _db.FilprideDeliveryReceipts
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(po => po!.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Include(dr => dr.Hauler)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos!.PickUpPoint)
                .Include(dr => dr.Customer)
                .Include(dr => dr.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos!.Commissionee)
                .FirstOrDefaultAsync(cancellationToken);

            var freight = deliveryReceipt?.CustomerOrderSlip?.DeliveryOption == SD.DeliveryOption_DirectDelivery
                ? deliveryReceipt.Freight
                : 0;

            existingRecord.Date = viewModel.Date;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.StationCode = stationCodeClaim;
            existingRecord.GainOrLoss = viewModel.QuantityReceived - viewModel.QuantityDelivered;
            existingRecord.PurchaseOrderNo = existingPo.PurchaseOrderNo;
            existingRecord.Type = existingPo.Type;
            existingRecord.PurchaseOrderId = viewModel.PurchaseOrderId;
            existingRecord.ReceivedDate = viewModel.ReceivedDate;
            existingRecord.SupplierInvoiceNumber = viewModel.SupplierInvoiceNumber;
            existingRecord.SupplierInvoiceDate = viewModel.SupplierInvoiceDate;
            existingRecord.SupplierDrNo = viewModel.SupplierDrNo;
            existingRecord.WithdrawalCertificate = viewModel.WithdrawalCertificate;
            existingRecord.TruckOrVessels = viewModel.TruckOrVessels;
            existingRecord.QuantityDelivered = viewModel.QuantityDelivered;
            existingRecord.QuantityReceived = viewModel.QuantityReceived;
            existingRecord.AuthorityToLoadNo = viewModel.AuthorityToLoadNo;
            existingRecord.Amount = viewModel.QuantityReceived * (existingPo.UnitPrice + freight);

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

        public async Task AutoGenerateReceivingReport(FilprideDeliveryReceipt deliveryReceipt, DateOnly deliveredDate, CancellationToken cancellationToken = default)
        {
            var getPurchaseOrder = await _db.MobilityPurchaseOrders
                .Include(p => p.PickUpPoint)
                .FirstOrDefaultAsync(p => p.PurchaseOrderNo == deliveryReceipt.CustomerOrderSlip!.CustomerPoNo, cancellationToken);

            MobilityReceivingReport model = new()
            {
                Date = deliveredDate,
                PurchaseOrderId = getPurchaseOrder!.PurchaseOrderId,
                PurchaseOrderNo = getPurchaseOrder.PurchaseOrderNo,
                QuantityDelivered = deliveryReceipt.Quantity,
                QuantityReceived = deliveryReceipt.Quantity,
                TruckOrVessels = getPurchaseOrder.PickUpPoint!.Depot,
                AuthorityToLoadNo = deliveryReceipt.AuthorityToLoadNo,
                Remarks = "PENDING",
                CreatedBy = "SYSTEM GENERATED",
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                PostedBy = "SYSTEM GENERATED",
                PostedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                Status = nameof(Status.Posted),
                Type = getPurchaseOrder.Type,
                StationCode = getPurchaseOrder.StationCode,
            };

            if (model.QuantityDelivered > getPurchaseOrder.Quantity - getPurchaseOrder.QuantityReceived)
            {
                throw new ArgumentException($"The inputted quantity exceeds the remaining delivered quantity for Purchase Order: " +
                                            $"{getPurchaseOrder.PurchaseOrderNo}. " +
                                            "Please contact the TNS department to verify the appointed supplier.");
            }

            var freight = deliveryReceipt.CustomerOrderSlip!.DeliveryOption == SD.DeliveryOption_DirectDelivery
                ? deliveryReceipt.Freight
                : 0;

            model.ReceivedDate = model.Date;
            model.ReceivingReportNo = await GenerateCodeAsync(model.StationCode, model.Type, cancellationToken);
            model.DueDate = await ComputeDueDateAsync(model.PurchaseOrder!.Terms, model.Date, cancellationToken);
            model.GainOrLoss = model.QuantityDelivered - model.QuantityReceived;
            model.Amount = model.QuantityReceived * (getPurchaseOrder.UnitPrice + freight);

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailCreate = new(model.PostedBy,
                $"Created new receiving report# {model.ReceivingReportNo}",
                "Receiving Report",
                nameof(Mobility));

            FilprideAuditTrail auditTrailPost = new(model.PostedBy,
                $"Posted receiving report# {model.ReceivingReportNo}",
                "Receiving Report",
                nameof(Mobility));

            await _db.AddAsync(auditTrailCreate, cancellationToken);
            await _db.AddAsync(auditTrailPost, cancellationToken);

            #endregion --Audit Trail Recording

            await _db.AddAsync(model, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await PostAsync(model, cancellationToken);
        }

        public async Task<int> RemoveQuantityReceived(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _db.MobilityPurchaseOrders
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
            if (po.QuantityReceived > po.Quantity)
            {
                throw new ArgumentException("Input is exceed to remaining quantity received");
            }

            return await _db.SaveChangesAsync(cancellationToken);

        }

        private async Task UpdatePoAsync(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _db.MobilityPurchaseOrders
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
    }
}
