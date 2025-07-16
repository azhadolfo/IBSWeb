using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ICustomerBranchRepository : IRepository<FilprideCustomerBranch>
    {
        Task UpdateAsync(FilprideCustomerBranch model, CancellationToken cancellationToken);
    }
}
