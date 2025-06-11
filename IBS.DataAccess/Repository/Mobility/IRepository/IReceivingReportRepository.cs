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

        Task UpdateAsync(ReceivingReportViewModel viewModel, string stationCodeClaim, CancellationToken cancellationToken);

        Task AutoGenerateReceivingReport(FilprideDeliveryReceipt deliveryReceipt, DateOnly deliveredDate, CancellationToken cancellationToken = default);

        Task<DateOnly> ComputeDueDateAsync(int poId, DateOnly rrDate, CancellationToken cancellationToken = default);

        Task<int> RemoveQuantityReceived(int id, decimal quantityReceived, CancellationToken cancellationToken = default);
    }
}
