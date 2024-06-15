using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IReceivingReportRepository : IRepository<FilprideReceivingReport>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task<DateOnly> CalculateDueDateAsync(string terms, DateOnly transactionDate, CancellationToken cancellationToken = default);
    }
}