using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;

namespace IBS.DataAccess.Repository.Mobility
{
    public class PurchaseOrderRepository : Repository<MobilityPurchaseOrder>, IPurchaseOrderRepository
    {
        private readonly ApplicationDbContext _db;

        public PurchaseOrderRepository(ApplicationDbContext db) : base(db)
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

        private async Task<string> GenerateCodeForDocumented(string stationCode, CancellationToken cancellationToken)
        {
            var lastPo = await _db
                .MobilityPurchaseOrders
                .Where(s => s.StationCode == stationCode && s.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.PurchaseOrderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastPo == null)
            {
                return $"{stationCode}-PO000000001";
            }

            var lastSeries = lastPo.PurchaseOrderNo;
            var numericPart = lastSeries.Substring(6);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return $"{lastSeries.Substring(0, 6) + incrementedNumber.ToString("D9")}";

        }

        private async Task<string> GenerateCodeForUnDocumented(string stationCode, CancellationToken cancellationToken)
        {
            var lastPo = await _db
                .MobilityPurchaseOrders
                .Where(s => s.StationCode == stationCode && s.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.PurchaseOrderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastPo == null)
            {
                return $"{stationCode}-POU00000001";
            }

            var lastSeries = lastPo.PurchaseOrderNo;
            var numericPart = lastSeries.Substring(7);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return $"{lastSeries.Substring(0, 7) + incrementedNumber.ToString("D8")}";
        }

        public async Task PostAsync(MobilityPurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
        {
            ///TODO PENDING process the method here

            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MobilityPurchaseOrder>> GetAllAsync(Expression<Func<MobilityPurchaseOrder, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityPurchaseOrder> query = dbSet
                .Include(po => po.Product)
                .Include(po => po.Supplier)
                .Include(po => po.PickUpPoint);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<MobilityPurchaseOrder?> GetAsync(Expression<Func<MobilityPurchaseOrder, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(po => po.Product)
                .Include(po => po.Supplier)
                .Include(po => po.PickUpPoint)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingRecord = await _db.MobilityPurchaseOrders
                .FirstOrDefaultAsync(x => x.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken)
                                 ?? throw new NullReferenceException("Purchase order not found");

            existingRecord.Date = viewModel.Date;
            existingRecord.SupplierId = viewModel.SupplierId;
            existingRecord.ProductId = viewModel.ProductId;
            existingRecord.Quantity = viewModel.Quantity;
            existingRecord.UnitPrice = viewModel.UnitPrice;
            existingRecord.Amount = viewModel.Quantity * viewModel.UnitPrice;
            //existingRecord.TotalAmount = existingRecord.Amount - viewModel.Discount;
            existingRecord.Remarks = viewModel.Remarks;

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
    }
}
