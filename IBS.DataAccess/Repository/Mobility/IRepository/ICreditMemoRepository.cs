using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ICreditMemoRepository : IRepository<MobilityCreditMemo>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);
    }
}
