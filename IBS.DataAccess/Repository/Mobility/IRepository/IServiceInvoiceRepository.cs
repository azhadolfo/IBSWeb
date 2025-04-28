using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.MasterFile;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IServiceInvoiceRepository : IRepository<MobilityServiceInvoice>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);
    }
}
