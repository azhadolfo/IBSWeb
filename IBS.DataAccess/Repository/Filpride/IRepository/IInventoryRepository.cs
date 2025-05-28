using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IInventoryRepository : IRepository<FilprideInventory>
    {
        Task<bool> HasAlreadyBeginningInventory(int productId, int poId, string company, CancellationToken cancellationToken = default);

        Task AddBeginningInventory(BeginningInventoryViewModel viewModel, string company, CancellationToken cancellationToken = default);

        Task AddPurchaseToInventoryAsync(FilprideReceivingReport receivingReport, CancellationToken cancellationToken = default);

        Task AddSalesToInventoryAsync(FilprideDeliveryReceipt deliveryReceipt, CancellationToken cancellationToken = default);

        Task AddActualInventory(ActualInventoryViewModel viewModel, string company, CancellationToken cancellationToken = default);

        Task VoidInventory(FilprideInventory model, CancellationToken cancellationToken = default);

        Task ReCalculateInventoryAsync(List<FilprideInventory> inventories, CancellationToken cancellationToken = default);
    }
}
