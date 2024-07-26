using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.DataAccess.Repository.Mobility.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Product { get; }

        ICompanyRepository Company { get; }

        IChartOfAccountRepository ChartOfAccount { get; }

        Task SaveAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncByNo(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncById(CancellationToken cancellationToken = default);

        #region--Mobility

        IFuelPurchaseRepository MobilityFuelPurchase { get; }

        ILubePurchaseHeaderRepository MobilityLubePurchaseHeader { get; }

        ILubePurchaseDetailRepository MobilityLubePurchaseDetail { get; }

        ISalesHeaderRepository MobilitySalesHeader { get; }

        ISalesDetailRepository MobilitySalesDetail { get; }

        IPOSalesRepository MobilityPurchaseOrder { get; }

        IOfflineRepository MobilityOffline { get; }

        IStationRepository MobilityStation { get; }

        IInventoryRepository MobilityInventory { get; }

        IGeneralLedgerRepository MobilityGeneralLedger { get; }

        Task<List<SelectListItem>> GetMobilityStationListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityStationListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityCustomerListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityCustomerListAsyncByCode(CancellationToken cancellationToken = default);

        #endregion

        #region--Filpride

        IPurchaseOrderRepository FilpridePurchaseOrder { get; }
        IReceivingReportRepository FilprideReceivingReport { get; }
        ICustomerOrderSlipRepository FilprideCustomerOrderSlip { get; }
        IDeliveryReceiptRepository FilprideDeliveryReceipt { get; }
        ISupplierRepository FilprideSupplier { get; }
        ICustomerRepository FilprideCustomer { get; }
        IHaulerRepository FilprideHauler { get; }

        Task<List<SelectListItem>> GetFilprideCustomerListAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideSupplierListAsyncById(CancellationToken cancellationToken = default);

        #endregion
    }
}