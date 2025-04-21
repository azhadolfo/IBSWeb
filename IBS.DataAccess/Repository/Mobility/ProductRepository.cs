using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class ProductRepository : Repository<MobilityProduct>, IProductRepository
    {
        private ApplicationDbContext _db;

        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<bool> IsProductExist(string product, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityProducts
                .AnyAsync(p => p.ProductName == product, cancellationToken);
        }

        public async Task UpdateAsync(MobilityProduct model, CancellationToken cancellationToken = default)
        {
            MobilityProduct existingProduct = await _db
                .MobilityProducts
                .FindAsync(model.ProductId, cancellationToken) ?? throw new InvalidOperationException($"Product with id '{model.ProductId}' not found.");

            existingProduct.ProductCode = model.ProductCode;
            existingProduct.ProductName = model.ProductName;
            existingProduct.ProductUnit = model.ProductUnit;

            if (_db.ChangeTracker.HasChanges())
            {
                existingProduct.EditedBy = model.EditedBy;
                existingProduct.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetProductListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityProducts
                .OrderBy(p => p.ProductId)
                .Where(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = p.ProductCode + " " + p.ProductName
                })
                .ToListAsync(cancellationToken);
        }
    }
}
