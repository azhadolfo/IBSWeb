using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.Integrated;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IAuthorityToLoadRepository : IRepository<FilprideAuthorityToLoad>
    {
        Task<string> GenerateAtlNo(string company, CancellationToken cancellationToken);
    }
}
