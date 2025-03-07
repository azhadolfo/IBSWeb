using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility;
using IBS.Utility.Helpers;

namespace IBS.DataAccess.Repository.Mobility
{
    public class PurchaseOrderRepository : Repository<MobilityPurchaseOrder>, IPurchaseOrderRepository
    {
        private ApplicationDbContext _db;

        public PurchaseOrderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string stationCode, CancellationToken cancellationToken = default)
        {
            MobilityPurchaseOrder? lastPo = await _db
                .MobilityPurchaseOrders
                .Where(s => s.StationCode == stationCode)
                .OrderBy(c => c.PurchaseOrderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastPo != null)
            {
                string lastSeries = lastPo.PurchaseOrderNo.Substring(lastPo.PurchaseOrderNo.IndexOf('-') + 3);
                int incrementedNumber = int.Parse(lastSeries) + 1;

                return $"{stationCode}-PO{incrementedNumber:D5}";
            }
            else
            {
                return $"{stationCode}-PO00001"; //S07-PO00001
            }
        }

        public async Task PostAsync(MobilityPurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
        {
            //PENDING process the method here

            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MobilityPurchaseOrder>> GetAllAsync(Expression<Func<MobilityPurchaseOrder, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityPurchaseOrder> query = dbSet
                .Include(po => po.Product)
                .Include(po => po.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<MobilityPurchaseOrder> GetAsync(Expression<Func<MobilityPurchaseOrder, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(po => po.Product)
                .Include(po => po.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingRecord = await _db.MobilityPurchaseOrders
                .FindAsync(viewModel.PurchaseOrderId, cancellationToken);

            existingRecord.Date = viewModel.Date;
            existingRecord.SupplierId = viewModel.SupplierId;
            existingRecord.ProductId = viewModel.ProductId;
            existingRecord.Quantity = viewModel.Quantity;
            existingRecord.UnitPrice = viewModel.UnitPrice;
            existingRecord.Amount = viewModel.Quantity * viewModel.UnitPrice;
            existingRecord.Discount = viewModel.Discount;
            existingRecord.TotalAmount = existingRecord.Amount - viewModel.Discount;
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
