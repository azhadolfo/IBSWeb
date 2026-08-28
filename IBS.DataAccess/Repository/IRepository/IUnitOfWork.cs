using IBS.DataAccess.Repository.Bienes.IRepository;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        #region--Filpride

        Filpride.IRepository.IChartOfAccountRepository FilprideChartOfAccount { get; }
        Filpride.IRepository.ICustomerOrderSlipRepository FilprideCustomerOrderSlip { get; }
        IDeliveryReceiptRepository FilprideDeliveryReceipt { get; }
        Filpride.IRepository.ISupplierRepository FilprideSupplier { get; }
        Filpride.IRepository.ICustomerRepository FilprideCustomer { get; }
        IAuditTrailRepository FilprideAuditTrail { get; }
        ICustomerBranchRepository FilprideCustomerBranch { get; }
        ITermsRepository FilprideTerms { get; }
        Filpride.IRepository.IGeneralLedgerRepository GeneralLedger { get; }
        IProvisionalReceiptRepository ProvisionalReceipt { get; }
        ILockedPeriodAdjustmentRepository LockedPeriodAdjustment { get; }
        IDepartmentAccessRepository DepartmentAccess { get; }

        Task<List<SelectListItem>> GetFilprideCustomerListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideEmployeeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideNonTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideCommissioneeListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideHaulerListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideBankAccountListById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetDistinctFilpridePickupPointListById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideServiceListById(CancellationToken cancellationToken = default);

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

        Task<bool> IsPeriodPostedAsync(DateOnly date, CancellationToken cancellationToken = default);

        Task<DateTime> GetMinimumPeriodBasedOnThePostedPeriods(Module module, CancellationToken cancellationToken = default);

        Task<bool> IsPeriodPostedAsync(Module module, DateOnly date, CancellationToken cancellationToken = default);
    }
}
