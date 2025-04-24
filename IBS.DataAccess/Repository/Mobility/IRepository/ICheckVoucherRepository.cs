using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ICheckVoucherRepository : IRepository<MobilityCheckVoucherHeader>
    {
        Task<string> GenerateCodeAsync(string stationCode, string type, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeMultipleInvoiceAsync(string stationCodeClaims, string type,
            CancellationToken cancellationToken = default);

        Task<string> GenerateCodeMultiplePaymentAsync(string stationCodeClaims, string type,
            CancellationToken cancellationToken = default);
    }
}
