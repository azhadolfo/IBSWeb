using IBS.DataAccess.Repository.IRepository;
using IBS.DTOs;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IMsapChartOfAccountRepository : IRepository<MsapChartOfAccount>
    {
        Task<List<SelectListItem>> GetMainAccount(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMemberAccount(string parentAcc, CancellationToken cancellationToken = default);

        Task<MsapChartOfAccount> GenerateAccount(MsapChartOfAccount model, string thirdLevel, CancellationToken cancellationToken = default);

        Task UpdateAsync(MsapChartOfAccount model, CancellationToken cancellationToken = default);

        // IEnumerable<ChartOfAccountDto> GetSummaryReportView(CancellationToken cancellationToken = default);
    }
}