using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.MasterFile;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IPrincipalRepository : IRepository<Principal>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<Principal>> SearchPrincipalsAsync(string term, int customerId, int limit, CancellationToken cancellationToken);
    }
}


