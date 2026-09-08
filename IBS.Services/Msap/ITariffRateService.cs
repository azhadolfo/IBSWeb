using IBS.Models.Msap;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Services.Msap
{
    public interface ITariffRateService
    {
        Task<IEnumerable<TariffRate>> GetAllAsync(CancellationToken cancellationToken);
        Task<TariffRate?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<TariffRate> PopulateSelectListsAsync(TariffRate? model, CancellationToken cancellationToken);
        Task<List<SelectListItem>> GetTerminalsByPortAsync(int portId, CancellationToken cancellationToken);
        Task<ServiceResult<int>> UpsertAsync(TariffRate model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken);
    }
}
