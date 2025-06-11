using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsReceivable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ICreditMemoRepository : IRepository<FilprideCreditMemo>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);
    }
}
