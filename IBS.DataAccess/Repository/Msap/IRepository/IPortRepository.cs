using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IPortRepository : IRepository<Port>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMsapPortsSelectList(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMsapPortsById(CancellationToken cancellationToken = default);
    }
}


