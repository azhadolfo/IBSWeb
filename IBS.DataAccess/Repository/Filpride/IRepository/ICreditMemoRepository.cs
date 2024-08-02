using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsReceivable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ICreditMemoRepository : IRepository<FilprideCreditMemo>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);
    }
}