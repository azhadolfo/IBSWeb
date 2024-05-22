using IBS.Models;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IOfflineRepository : IRepository<Offline>
    {
        Task<List<SelectListItem>> GetOfflineListAsync(CancellationToken cancellationToken = default);

        Task<Offline> GetOffline(int offlineId, CancellationToken cancellationToken = default);

        Task InsertEntry(AdjustReportViewModel model, CancellationToken cancellationToken = default);
    }
}
