using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IReceivingReportRepo : IRepository<ReceivingReport>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task<int> UpdatePOAsync(int id, decimal quantityReceived, CancellationToken cancellationToken = default);

        Task<int> RemoveQuantityReceived(int id, decimal quantityReceived, CancellationToken cancellationToken = default);

        Task<DateOnly> ComputeDueDateAsync(int poId, DateOnly rrDate, CancellationToken cancellationToken = default);
    }
}