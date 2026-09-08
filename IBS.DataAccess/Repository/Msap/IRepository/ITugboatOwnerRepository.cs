using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface ITugboatOwnerRepository : IRepository<TugboatOwner>
    {
        Task<List<SelectListItem>> GetMsapTugboatOwnersSelectList(CancellationToken cancellationToken = default);
    }
}


