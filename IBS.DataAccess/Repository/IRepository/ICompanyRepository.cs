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
        Task<bool> IsCompanyExistAsync(string companyName, CancellationToken cancellationToken = default);

        Task<bool> IsTinNoExistAsync(string tinNo, CancellationToken cancellationToken = default);

        Task UpdateAsync(Company model, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);
    }
}