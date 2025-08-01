using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ICustomerOrderSlipRepository : IRepository<FilprideCustomerOrderSlip>
    {
        Task<string> GenerateCodeAsync(string companyClaims, CancellationToken cancellationToken = default);

        Task UpdateAsync(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCosListNotDeliveredAsync(string companyClaims, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCosListPerCustomerAsync(int customerId, CancellationToken cancellationToken = default);

        Task<decimal> GetCustomerCreditBalance(int customerId, CancellationToken cancellationToken = default);
    }
}
