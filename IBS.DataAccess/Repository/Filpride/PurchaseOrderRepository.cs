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
                .Where(po => po.PostedBy != null && !po.IsServed)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingRecord = await _db.FilpridePurchaseOrders
                .FindAsync(viewModel.PurchaseOrderId, cancellationToken);

            existingRecord.Date = viewModel.Date;
            existingRecord.SupplierId = viewModel.SupplierId;
            existingRecord.ProductId = viewModel.ProductId;
            existingRecord.Terms = viewModel.Terms;
            existingRecord.Port = viewModel.Port;
            existingRecord.Quantity = viewModel.Quantity;
            existingRecord.UnitCost = viewModel.UnitCost;
            existingRecord.TotalAmount = viewModel.Quantity * viewModel.UnitCost;
            existingRecord.Remarks = viewModel.Remarks;

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = viewModel.CurrentUser;
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