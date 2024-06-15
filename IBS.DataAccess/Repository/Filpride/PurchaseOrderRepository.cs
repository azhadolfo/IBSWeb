using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class PurchaseOrderRepository : Repository<FilpridePurchaseOrder>, IPurchaseOrderRepository
    {
        private ApplicationDbContext _db;

        public PurchaseOrderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilpridePurchaseOrder? lastPo = await _db
                .FilpridePurchaseOrders
                .OrderBy(c => c.PurchaseOrderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastPo != null)
            {
                string lastSeries = lastPo.PurchaseOrderNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "PO0000000001";
            }
        }

        public override async Task<IEnumerable<FilpridePurchaseOrder>> GetAllAsync(Expression<Func<FilpridePurchaseOrder, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilpridePurchaseOrder> query = dbSet
                .Include(po => po.Supplier)
                .Include(po => po.Product);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilpridePurchaseOrder> GetAsync(Expression<Func<FilpridePurchaseOrder, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(po => po.Supplier)
                .Include(po => po.Product)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsync(CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePurchaseOrders
                .OrderBy(po => po.PurchaseOrderId)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(PurchaseOrderViewModel model, string userName, CancellationToken cancellationToken)
        {
            FilpridePurchaseOrder existingRecord = await _db.FilpridePurchaseOrders
                .FindAsync(model.PurchaseOrderId, cancellationToken);

            existingRecord.Date = model.Date;
            existingRecord.SupplierId = model.SupplierId;
            existingRecord.ProductId = model.ProductId;
            existingRecord.Terms = model.Terms;
            existingRecord.Quantity = model.Quantity;
            existingRecord.UnitCost = model.UnitCost;
            existingRecord.TotalAmount = model.Quantity * model.UnitCost;
            existingRecord.Remarks = model.Remarks;

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = userName;
                existingRecord.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }
    }
}