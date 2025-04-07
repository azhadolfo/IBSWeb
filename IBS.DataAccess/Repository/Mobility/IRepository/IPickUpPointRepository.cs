using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IPickUpPointRepository : IRepository<MobilityPickUpPoint>
    {
        Task<List<SelectListItem>> GetFilprideTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);
    }
}
