using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.DataAccess.Repository.Mobility.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        ICustomerRepository Customer { get; }

        IProductRepository Product { get; }

        ICompanyRepository Company { get; }

        IStationRepository Station { get; }

        IGeneralLedgerRepository GeneralLedger { get; }

        ISupplierRepository Supplier { get; }

        IInventoryRepository Inventory { get; }

        IChartOfAccountRepository ChartOfAccount { get; }

        Task SaveAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCustomerListAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetStationListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetStationListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncByNo(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetSupplierListAsyncById(CancellationToken cancellationToken = default);

        #region--Mobility

        IFuelPurchaseRepository MobilityFuelPurchase { get; }

        ILubePurchaseHeaderRepository MobilityLubePurchaseHeader { get; }

        ILubePurchaseDetailRepository MobilityLubePurchaseDetail { get; }

        ISalesHeaderRepository MobilitySalesHeader { get; }

        ISalesDetailRepository MobilitySalesDetail { get; }

        IPOSalesRepository MobilityPurchaseOrder { get; }

        IOfflineRepository MobilityOffline { get; }

        #endregion

        #region--Filpride

        IPurchaseOrderRepository FilpridePurchaseOrder { get; }

        #endregion
    }
}