using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface ICompanyRepository : IRepository<Company>
    {
        Task<bool> IsCompanyExistAsync(string companyName);

        Task<bool> IsTinNoExistAsync(string tinNo);

        Task UpdateAsync(Company model);

        Task<string> GenerateCodeAsync();
    }
}