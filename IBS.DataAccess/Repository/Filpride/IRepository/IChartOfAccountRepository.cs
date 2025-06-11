using IBS.DataAccess.Repository.IRepository;
using IBS.DTOs;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IChartOfAccountRepository : IRepository<FilprideChartOfAccount>
    {
        Task<List<SelectListItem>> GetMainAccount(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMemberAccount(string parentAcc, CancellationToken cancellationToken = default);

        Task<FilprideChartOfAccount> GenerateAccount(FilprideChartOfAccount model, string thirdLevel, CancellationToken cancellationToken = default);

        Task UpdateAsync(FilprideChartOfAccount model, CancellationToken cancellationToken = default);

        IEnumerable<ChartOfAccountDto> GetSummaryReportView(CancellationToken cancellationToken = default);
    }
}
