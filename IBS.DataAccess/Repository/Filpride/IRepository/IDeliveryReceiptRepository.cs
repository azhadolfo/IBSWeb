using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IDeliveryReceiptRepository : IRepository<FilprideDeliveryReceipt>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task UpdateAsync(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetDeliveryReceiptListAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetDeliveryReceiptListForSalesInvoice(int cosId, CancellationToken cancellationToken = default);

        Task PostAsync(FilprideDeliveryReceipt deliveryReceipt, CancellationToken cancellationToken = default);

        Task DeductTheVolumeToCos(int cosId, decimal drVolume, CancellationToken cancellationToken = default);

        Task UpdatePreviousAppointedSupplierAsync(FilprideDeliveryReceipt model);

        Task AssignNewPurchaseOrderAsync(DeliveryReceiptViewModel viewModel, FilprideDeliveryReceipt model);

        Task AutoReversalEntryForInTransit(CancellationToken cancellationToken = default);
    }
}
