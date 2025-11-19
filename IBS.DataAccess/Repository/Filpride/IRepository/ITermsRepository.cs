using System.Linq.Expressions;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ITermsRepository : IRepository<FilprideTerms>
    {
        Task UpdateAsync(FilprideTerms model, CancellationToken cancellationToken = default);
    }
}
