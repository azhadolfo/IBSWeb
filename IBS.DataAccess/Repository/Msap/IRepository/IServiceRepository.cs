using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IMsapMaritimeServiceRepository : IRepository<Service>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMsapActivitiesServicesById(CancellationToken cancellationToken = default);
    }
}


