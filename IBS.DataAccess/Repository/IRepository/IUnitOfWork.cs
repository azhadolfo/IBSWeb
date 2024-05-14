using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        ICustomerRepository Customer { get; }

        IProductRepository Product { get; }

        ICompanyRepository Company { get; }

        ISalesHeaderRepository SalesHeader { get; }

        ISalesDetailRepository SalesDetail { get; }

        IStationRepository Station { get; }

        IGeneralLedgerRepository GeneralLedger { get; }

        IFuelPurchaseRepository FuelPurchase { get; }

        ILubePurchaseHeaderRepository LubePurchaseHeader { get; }

        ILubePurchaseDetailRepository LubePurchaseDetail { get; }

        ISupplierRepository Supplier { get; }

        IInventoryRepository Inventory { get; }

        IChartOfAccountRepository ChartOfAccount { get; }

        IPOSalesRepository PurchaseOrder { get; }

        Task SaveAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCustomerListAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetStationListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetStationListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncByNo(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetOfflineListAsync(CancellationToken cancellationToken = default);

    }
}