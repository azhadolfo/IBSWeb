using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface ITerminalRepository : IRepository<MMSITerminal>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>>
            GetMMSITerminalsSelectList(int portId, CancellationToken cancellationToken = default);
    }
}
