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

        Task<List<SelectListItem>> GetCompanyListAsyncByName(CancellationToken cancellationToken = default);

        #region--Mobility

        IFuelPurchaseRepository MobilityFuelPurchase { get; }

        ILubePurchaseHeaderRepository MobilityLubePurchaseHeader { get; }

        ILubePurchaseDetailRepository MobilityLubePurchaseDetail { get; }

        ISalesHeaderRepository MobilitySalesHeader { get; }

        ISalesDetailRepository MobilitySalesDetail { get; }

        IPOSalesRepository MobilityPOSales { get; }

        IOfflineRepository MobilityOffline { get; }

        IStationRepository MobilityStation { get; }

        Mobility.IRepository.IInventoryRepository MobilityInventory { get; }

        IGeneralLedgerRepository MobilityGeneralLedger { get; }

        Mobility.IRepository.IPurchaseOrderRepository MobilityPurchaseOrder { get; }
        Mobility.IRepository.IReceivingReportRepository MobilityReceivingReport { get; }

        Task<List<SelectListItem>> GetMobilityStationListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityStationListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityCustomerListAsyncByCodeName(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityCustomerListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilitySupplierListAsyncById(CancellationToken cancellationToken = default);

        #endregion

        #region--Filpride
        ICustomerOrderSlipRepository FilprideCustomerOrderSlip { get; }
        IDeliveryReceiptRepository FilprideDeliveryReceipt { get; }
        ISupplierRepository FilprideSupplier { get; }
        ICustomerRepository FilprideCustomer { get; }
        IHaulerRepository FilprideHauler { get; }

        Task<List<SelectListItem>> GetFilprideCustomerListAsync(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        #endregion

        #region AAS

        #region Accounts Receivable
        ISalesInvoiceRepository FilprideSalesInvoice { get; }

        IServiceInvoiceRepository FilprideServiceInvoice { get; }

        ICollectionReceiptRepository FilprideCollectionReceipt { get; }

        IDebitMemoRepository FilprideDebitMemo { get; }

        ICreditMemoRepository FilprideCreditMemo { get; }
        #endregion

        #region Accounts Payable

        ICheckVoucherRepository FilprideCheckVoucher { get; }

        IJournalVoucherRepository FilprideJournalVoucher { get; }

        Filpride.IRepository.IPurchaseOrderRepository FilpridePurchaseOrder { get; }

        Filpride.IRepository.IReceivingReportRepository FilprideReceivingReport { get; }

        #endregion

        #region Books and Report
        Filpride.IRepository.IInventoryRepository FilprideInventory { get; }

        IReportRepository FilprideReport { get; }
        #endregion

        #region Master File

        IBankAccountRepository FilprideBankAccount { get; }

        IServiceRepository FilprideService { get; }

        IPickUpPointRepository FilpridePickUpPoint { get; }

        #endregion

        #endregion
    }
}