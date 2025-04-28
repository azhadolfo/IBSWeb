using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ISupplierRepository : IRepository<MobilitySupplier>
    {
        Task<string> GenerateCodeAsync(string stationCodeClaims, CancellationToken cancellationToken = default);

        Task UpdateAsync(MobilitySupplier model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityTradeSupplierListAsyncById(string stationCodeClaims, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetmobilityTradeSupplierListAsyncById(string stationCode,
            CancellationToken cancellationToken = default);
    }
}
