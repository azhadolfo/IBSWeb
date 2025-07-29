using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Integrated;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IReceivingReportRepository : IRepository<FilprideReceivingReport>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);

        Task<int> RemoveQuantityReceived(int id, decimal quantityReceived, CancellationToken cancellationToken = default);

        Task<DateOnly> ComputeDueDateAsync(int poId, DateOnly rrDate, CancellationToken cancellationToken = default);

        Task<string> AutoGenerateReceivingReport(FilprideDeliveryReceipt deliveryReceipt, DateOnly liftingDate, string userName, CancellationToken cancellationToken = default);

        Task PostAsync(FilprideReceivingReport model, CancellationToken cancellationToken = default);

        Task VoidReceivingReportAsync(int receivingReportId, string currentUser, CancellationToken cancellationToken = default);
    }
}
