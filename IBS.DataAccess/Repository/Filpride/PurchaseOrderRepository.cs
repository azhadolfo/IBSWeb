using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using IBS.Models.ViewModels;
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

        //TODO Edit functionality of Purchase Order
        public async Task Update(PurchaseOrderViewModel model, CancellationToken cancellationToken)
        {
            FilpridePurchaseOrder existingRecord = await _db.FilpridePurchaseOrders
                .FindAsync(model.PurchaseOrderId, cancellationToken);

            existingRecord.Date = model.Date;
        }
    }
}