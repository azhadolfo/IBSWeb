using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface ITariffTableRepository : IRepository<MMSITariffRate>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSIPortsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITerminalsById(int portId, CancellationToken cancellationToken = default);
    }
}
