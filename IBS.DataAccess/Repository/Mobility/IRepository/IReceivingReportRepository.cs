using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IReceivingReportRepository : IRepository<MobilityReceivingReport>
    {
        Task<string> GenerateCodeAsync(string stationCode, CancellationToken cancellationToken = default);

        Task PostAsync(MobilityReceivingReport receivingReport, CancellationToken cancellationToken = default);

        Task UpdateAsync(ReceivingReportViewModel viewModel, CancellationToken cancellationToken);
    }
}