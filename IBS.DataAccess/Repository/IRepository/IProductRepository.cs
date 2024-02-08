using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<bool> IsProductExist(string product);

        Task UpdateAsync(Product model);
    }
}