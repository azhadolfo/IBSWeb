using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private ApplicationDbContext _db;

        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<bool> IsProductExist(string product, CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .AnyAsync(p => p.ProductName == product, cancellationToken);
        }

        public async Task UpdateAsync(Product model, CancellationToken cancellationToken = default)
        {
            Product existingProduct = await _db
                .Products
                .FindAsync(model.ProductId, cancellationToken) ?? throw new InvalidOperationException($"Product with id '{model.ProductId}' not found.");

            existingProduct.ProductCode = model.ProductCode;
            existingProduct.ProductName = model.ProductName;
            existingProduct.ProductUnit = model.ProductUnit;

            if (_db.ChangeTracker.HasChanges())
            {
                existingProduct.EditedBy = model.EditedBy;
                existingProduct.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }
    }
}