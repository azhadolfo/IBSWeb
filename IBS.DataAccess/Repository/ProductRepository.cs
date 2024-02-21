using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                .AnyAsync(p => p.ProductName == product,cancellationToken);
        }

        public async Task UpdateAsync(Product model, CancellationToken cancellationToken = default)
        {
            Product? existingProduct = await _db
                .Products
                .FindAsync(model.ProductId, cancellationToken);

            existingProduct!.ProductCode = model.ProductCode;
            existingProduct!.ProductName = model.ProductName;
            existingProduct!.ProductUnit = model.ProductUnit;
            existingProduct!.EditedBy = "Ako";
            existingProduct!.EditedDate = DateTime.Now;
        }
    }
}