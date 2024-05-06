using IBS.Models;
using IBS.Models.ViewModels;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface ISalesHeaderRepository : IRepository<SalesHeader>
    {
        Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default);

        Task UpdateAsync(SalesVM model, double[] closing, double[] opening, CancellationToken cancellationToken = default);

        IEnumerable<dynamic> GetSalesHeaderJoin(IEnumerable<SalesHeader> salesHeaders, CancellationToken cancellationToken = default);

        Task ImportSales(CancellationToken cancellationToken = default);
    }
}