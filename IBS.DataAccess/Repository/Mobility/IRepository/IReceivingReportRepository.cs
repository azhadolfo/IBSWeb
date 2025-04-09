using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IReceivingReportRepository : IRepository<MobilityReceivingReport>
    {
        Task<string> GenerateCodeAsync(string stationCode, string type, CancellationToken cancellationToken = default);

        Task PostAsync(MobilityReceivingReport receivingReport, CancellationToken cancellationToken = default);

        Task UpdateAsync(ReceivingReportViewModel viewModel, CancellationToken cancellationToken);

        Task<string> AutoGenerateReceivingReport(FilprideDeliveryReceipt deliveryReceipt, DateOnly liftingDate, CancellationToken cancellationToken = default);

        Task<DateOnly> ComputeDueDateAsync(int poId, DateOnly rrDate, CancellationToken cancellationToken = default);
    }
}
