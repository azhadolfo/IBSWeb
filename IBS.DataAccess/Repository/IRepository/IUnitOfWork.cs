using IBS.DataAccess.Repository.Bienes.IRepository;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using IBankAccountRepository = IBS.DataAccess.Repository.Filpride.IRepository.IBankAccountRepository;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        MasterFile.IRepository.IProductRepository Product { get; }

        ICompanyRepository Company { get; }

        Task SaveAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncByNo(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncByAccountTitle(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCompanyListAsyncByName(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCompanyListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCashierListAsyncByUsernameAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCashierListAsyncByStationAsync(CancellationToken cancellationToken = default);

        #region--Mobility

        Mobility.IRepository.IChartOfAccountRepository MobilityChartOfAccount { get; }

        IFuelPurchaseRepository MobilityFuelPurchase { get; }

        ILubePurchaseHeaderRepository MobilityLubePurchaseHeader { get; }

        ILubePurchaseDetailRepository MobilityLubePurchaseDetail { get; }

        ISalesHeaderRepository MobilitySalesHeader { get; }

        ISalesDetailRepository MobilitySalesDetail { get; }

        IPOSalesRepository MobilityPOSales { get; }

        IOfflineRepository MobilityOffline { get; }

        IStationRepository MobilityStation { get; }
        Mobility.IRepository.ISupplierRepository MobilitySupplier { get; }
        Mobility.IRepository.ICustomerRepository MobilityCustomer { get; }
        Mobility.IRepository.IBankAccountRepository MobilityBankAccount { get; }
        Mobility.IRepository.IServiceRepository MobilityService { get; }
        Mobility.IRepository.IProductRepository MobilityProduct { get; }
        Mobility.IRepository.IPickUpPointRepository MobilityPickUpPoint { get; }
        Mobility.IRepository.IEmployeeRepository MobilityEmployee { get; }
        Mobility.IRepository.IInventoryRepository MobilityInventory { get; }

        IGeneralLedgerRepository MobilityGeneralLedger { get; }

        Mobility.IRepository.IPurchaseOrderRepository MobilityPurchaseOrder { get; }

        Mobility.IRepository.IReceivingReportRepository MobilityReceivingReport { get; }
        Mobility.IRepository.ICheckVoucherRepository MobilityCheckVoucher { get; }
        Mobility.IRepository.IJournalVoucherRepository MobilityJournalVoucher { get; }
        Mobility.IRepository.IServiceInvoiceRepository MobilityServiceInvoice { get; }
        Mobility.IRepository.ICreditMemoRepository MobilityCreditMemo { get; }
        Mobility.IRepository.IDebitMemoRepository MobilityDebitMemo { get; }
        Mobility.IRepository.ICollectionReceiptRepository MobilityCollectionReceipt { get; }

        Mobility.IRepository.ICustomerOrderSlipRepository MobilityCustomerOrderSlip { get; }

        IDepositRepository MobilityDeposit { get; }

        Task<List<SelectListItem>> GetMobilityStationListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityStationListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityStationListWithCustomersAsyncByCode(List<MobilityCustomer> mobilityCustomers, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityCustomerListAsyncByCodeName(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityCustomerListAsyncById(string stationCodeClaims, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityCustomerListAsyncByIdAll(string stationCodeClaims, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityCustomerListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilitySupplierListAsyncById(string stationCodeClaims, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityCustomerListAsync(string stationCodeClaims, CancellationToken cancellationToken = default);
        Task<string> GetMobilityStationNameAsync(string stationCodeClaims, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityProductListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMobilityProductListAsyncById(CancellationToken cancellationToken = default);

        #endregion

        #region--Filpride

        Filpride.IRepository.IChartOfAccountRepository FilprideChartOfAccount { get; }
        Filpride.IRepository.ICustomerOrderSlipRepository FilprideCustomerOrderSlip { get; }
        IDeliveryReceiptRepository FilprideDeliveryReceipt { get; }
        Filpride.IRepository.ISupplierRepository FilprideSupplier { get; }
        Filpride.IRepository.ICustomerRepository FilprideCustomer { get; }
        IAuditTrailRepository FilprideAuditTrail { get; }

        Filpride.IRepository.IEmployeeRepository FilprideEmployee { get; }

        Task<List<SelectListItem>> GetFilprideCustomerListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideNonTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideCommissioneeListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideHaulerListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideBankAccountListById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideEmployeeListById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetDistinctFilpridePickupPointListById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideServiceListById(string company, CancellationToken cancellationToken = default);

        #endregion

        #region --MMSI

        IMsapRepository Msap { get; }
        IServiceRequestRepository ServiceRequest { get; }
        IDispatchTicketRepository DispatchTicket { get; }
        IBillingRepository Billing { get; }
        ICollectionRepository Collection { get; }
        IMMSIReportRepository MMSIReport { get; }
        ITariffTableRepository TariffTable { get; }
        IPortRepository Port { get; }
        IPrincipalRepository Principal { get; }
        ITerminalRepository Terminal { get; }
        ITugboatRepository Tugboat { get; }
        IVesselRepository Vessel { get; }

        #endregion

        #region AAS

        #region Accounts Receivable
        ISalesInvoiceRepository FilprideSalesInvoice { get; }

        Filpride.IRepository.IServiceInvoiceRepository FilprideServiceInvoice { get; }

        Filpride.IRepository.ICollectionReceiptRepository FilprideCollectionReceipt { get; }

        Filpride.IRepository.IDebitMemoRepository FilprideDebitMemo { get; }

        Filpride.IRepository.ICreditMemoRepository FilprideCreditMemo { get; }
        #endregion

        #region Accounts Payable

        Filpride.IRepository.ICheckVoucherRepository FilprideCheckVoucher { get; }

        Filpride.IRepository.IJournalVoucherRepository FilprideJournalVoucher { get; }

        Filpride.IRepository.IPurchaseOrderRepository FilpridePurchaseOrder { get; }

        Filpride.IRepository.IReceivingReportRepository FilprideReceivingReport { get; }

        #endregion

        #region Books and Report
        Filpride.IRepository.IInventoryRepository FilprideInventory { get; }

        IReportRepository FilprideReport { get; }
        #endregion

        #region Master File

        Filpride.IRepository.IBankAccountRepository FilprideBankAccount { get; }

        Filpride.IRepository.IServiceRepository FilprideService { get; }

        Filpride.IRepository.IPickUpPointRepository FilpridePickUpPoint { get; }

        IFreightRepository FilprideFreight { get; }

        IAuthorityToLoadRepository FilprideAuthorityToLoad { get; }

        #endregion

        #endregion

        #region --Bienes

        IPlacementRepository BienesPlacement { get; }

        #endregion

        INotificationRepository Notifications { get; }
    }
}
