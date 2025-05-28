using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models.Mobility.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IChartOfAccountRepository : IRepository<MobilityChartOfAccount>
    {
        Task<List<SelectListItem>> GetMainAccount(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMemberAccount(string parentAcc, CancellationToken cancellationToken = default);

        Task<MobilityChartOfAccount> GenerateAccount(MobilityChartOfAccount model, string thirdLevel, CancellationToken cancellationToken = default);

        Task UpdateAsync(MobilityChartOfAccount model, CancellationToken cancellationToken = default);

        IEnumerable<ChartOfAccountDto> GetSummaryReportView(CancellationToken cancellationToken = default);
    }
}
