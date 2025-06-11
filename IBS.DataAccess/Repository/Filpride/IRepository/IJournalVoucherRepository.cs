using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IJournalVoucherRepository : IRepository<FilprideJournalVoucherHeader>
    {
        Task<string> GenerateCodeAsync(string company, string? type, CancellationToken cancellationToken = default);
    }
}
