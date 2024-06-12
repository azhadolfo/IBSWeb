using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.ViewModels;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ISalesHeaderRepository : IRepository<SalesHeader>
    {
        Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default);

        Task UpdateAsync(SalesVM model, decimal[] closing, decimal[] opening, CancellationToken cancellationToken = default);

        IEnumerable<dynamic> GetSalesHeaderJoin(IEnumerable<SalesHeader> salesHeaders, CancellationToken cancellationToken = default);

        Task ComputeSalesPerCashier(bool hasPoSales, CancellationToken cancellationToken = default);
    }
}