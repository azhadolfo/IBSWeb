using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<bool> IsTinNoExistAsync(string tin);

        Task<string> GenerateCodeAsync(string customerType);

        Task UpdateAsync(Customer model);
    }
}