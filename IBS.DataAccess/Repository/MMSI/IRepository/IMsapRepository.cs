using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IMsapRepository
    {
        Task<List<SelectListItem>> GetMMSIUsersSelectListById(CancellationToken cancellationToken = default);
    }
}
