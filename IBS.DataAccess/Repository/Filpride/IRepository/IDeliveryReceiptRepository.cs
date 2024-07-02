using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IDeliveryReceiptRepository : IRepository<FilprideDeliveryReceipt>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task UpdateAsync(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetDeliveryReceiptListAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetDeliveryReceiptListByCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    }
}