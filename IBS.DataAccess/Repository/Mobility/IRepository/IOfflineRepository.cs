using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IOfflineRepository : IRepository<Offline>
    {
        Task<List<SelectListItem>> GetOfflineListAsync(CancellationToken cancellationToken = default);

        Task<Offline> GetOffline(int offlineId, CancellationToken cancellationToken = default);

        Task InsertEntry(AdjustReportViewModel model, CancellationToken cancellationToken = default);
    }
}
