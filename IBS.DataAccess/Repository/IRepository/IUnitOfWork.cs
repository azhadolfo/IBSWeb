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

        Task<List<SelectListItem>> GetCustomersAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductsAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductsAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetStationAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetStationAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountAsyncByNo(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountAsyncById(CancellationToken cancellationToken = default);

    }
}