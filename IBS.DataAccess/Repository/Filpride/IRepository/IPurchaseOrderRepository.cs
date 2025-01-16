using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Integrated;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IPurchaseOrderRepository : IRepository<FilpridePurchaseOrder>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetPurchaseOrderListAsyncByCode(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetPurchaseOrderListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetPurchaseOrderListAsyncBySupplier(int supplierId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetPurchaseOrderListAsyncBySupplierAndProduct(int supplierId, int productId, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeForSubPoAsync(string purchaseOrderNo, string company, CancellationToken cancellationToken = default);

        Task UpdateActualCostOnSalesAndReceiptsAsync(FilpridePOActualPrice model, CancellationToken cancellationToken = default);

        Task<List<string>> GetUntriggeredPurchaseOrderNumbersAsync(CancellationToken cancellationToken = default);

        Task UnlockTheCreationOfPurchaseOrderAsync(CancellationToken cancellationToken = default);
    }
}
