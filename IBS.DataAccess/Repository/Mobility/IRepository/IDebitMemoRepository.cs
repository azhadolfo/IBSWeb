using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IDebitMemoRepository : IRepository<MobilityDebitMemo>
    {
        Task<string> GenerateCodeAsync(string stationCode, string type, CancellationToken cancellationToken = default);
    }
}
