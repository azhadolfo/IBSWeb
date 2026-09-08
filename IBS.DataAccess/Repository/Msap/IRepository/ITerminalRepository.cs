using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface ITerminalRepository : IRepository<Terminal>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>?> GetMsapTerminalsSelectList(int? portId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMsapTerminalsById(DispatchTicket model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMsapAllTerminalsById(CancellationToken cancellationToken = default);
    }
}


