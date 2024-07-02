using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ICustomerOrderSlipRepository : IRepository<FilprideCustomerOrderSlip>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task UpdateAsync(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCosListAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCosListPerCustomerAsync(int customerId, CancellationToken cancellationToken = default);

        Task PostAsync(FilprideCustomerOrderSlip customerOrderSlip, CancellationToken cancellationToken = default);
    }
}