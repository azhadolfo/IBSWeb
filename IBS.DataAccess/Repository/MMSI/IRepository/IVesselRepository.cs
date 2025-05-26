using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IVesselRepository : IRepository<MMSIVessel>
    {
        Task<List<SelectListItem>> GetMMSIVesselsSelectList(CancellationToken cancellationToken = default);
    }
}
