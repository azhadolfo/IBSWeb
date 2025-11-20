using System.Linq.Expressions;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ITermsRepository : IRepository<FilprideTerms>
    {
        Task UpdateAsync(FilprideTerms model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideTermsListAsyncByCode(CancellationToken cancellationToken = default);
    }
}
