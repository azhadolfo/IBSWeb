using IBS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IChartOfAccountRepository : IRepository<ChartOfAccount>
    {
        Task<List<SelectListItem>> GetMainAccount(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMemberAccount(string parentAcc, CancellationToken cancellationToken = default);

        Task<ChartOfAccount> GenerateAccount(ChartOfAccount model, string thirdLevel, CancellationToken cancellationToken = default);

        Task UpdateAsync(ChartOfAccount model, CancellationToken cancellationToken = default);
    }
}
