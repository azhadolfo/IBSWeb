using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IJournalVoucherRepository : IRepository<MobilityJournalVoucherHeader>

    {
        Task<string> GenerateCodeAsync(string stationCode, string? type, CancellationToken cancellationToken = default);
    }
}
