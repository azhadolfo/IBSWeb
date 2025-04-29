using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ICollectionReceiptRepository : IRepository<MobilityCollectionReceipt>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);

        Task<List<MobilityOffsettings>> GetOffsettings(string source, string reference, string stationCode,
            CancellationToken cancellationToken = default);

        Task UpdateSV(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task RemoveSVPayment(int id, decimal paidAmount, decimal offsetAmount,
            CancellationToken cancellationToken = default);
    }
}
