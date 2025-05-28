using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IOfflineRepository : IRepository<MobilityOffline>
    {
        Task<List<SelectListItem>> GetOfflineListAsync(string stationCode, CancellationToken cancellationToken = default);

        Task<MobilityOffline?> GetOffline(int offlineId, CancellationToken cancellationToken = default);

        Task InsertEntry(AdjustReportViewModel model, CancellationToken cancellationToken = default);
    }
}
