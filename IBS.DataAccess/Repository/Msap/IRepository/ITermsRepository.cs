using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IMsapTermsRepository : IRepository<MsapTerms>
    {
        Task UpdateAsync(MsapTerms model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetTermsListAsyncByCode(CancellationToken cancellationToken = default);
    }
}