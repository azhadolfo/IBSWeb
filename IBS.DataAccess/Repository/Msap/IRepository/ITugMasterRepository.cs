using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface ITugMasterRepository : IRepository<TugMaster>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMsapTugMastersById(CancellationToken cancellationToken = default);
    }
}


