using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IDeliveryReportRepository : IRepository<FilprideDeliveryReport>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);
    }
}