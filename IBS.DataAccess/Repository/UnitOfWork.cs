using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Bienes;
using IBS.DataAccess.Repository.Bienes.IRepository;
using IBS.DataAccess.Repository.Filpride;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MasterFile;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.DataAccess.Repository.MMSI;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.DataAccess.Repository.Mobility;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BankAccountRepository = IBS.DataAccess.Repository.Mobility.BankAccountRepository;
using ChartOfAccountRepository = IBS.DataAccess.Repository.Mobility.ChartOfAccountRepository;
using CustomerOrderSlipRepository = IBS.DataAccess.Repository.Mobility.CustomerOrderSlipRepository;
using CustomerRepository = IBS.DataAccess.Repository.Mobility.CustomerRepository;
using IBankAccountRepository = IBS.DataAccess.Repository.Mobility.IRepository.IBankAccountRepository;
using IChartOfAccountRepository = IBS.DataAccess.Repository.Mobility.IRepository.IChartOfAccountRepository;
using ICustomerOrderSlipRepository = IBS.DataAccess.Repository.Mobility.IRepository.ICustomerOrderSlipRepository;
using ICustomerRepository = IBS.DataAccess.Repository.Mobility.IRepository.ICustomerRepository;
using IInventoryRepository = IBS.DataAccess.Repository.Mobility.IRepository.IInventoryRepository;
using InventoryRepository = IBS.DataAccess.Repository.Mobility.InventoryRepository;
using IPickUpPointRepository = IBS.DataAccess.Repository.Mobility.IRepository.IPickUpPointRepository;
using IProductRepository = IBS.DataAccess.Repository.MasterFile.IRepository.IProductRepository;
using IPurchaseOrderRepository = IBS.DataAccess.Repository.Mobility.IRepository.IPurchaseOrderRepository;
using IReceivingReportRepository = IBS.DataAccess.Repository.Mobility.IRepository.IReceivingReportRepository;
using IServiceRepository = IBS.DataAccess.Repository.Mobility.IRepository.IServiceRepository;
using ISupplierRepository = IBS.DataAccess.Repository.Mobility.IRepository.ISupplierRepository;
using PickUpPointRepository = IBS.DataAccess.Repository.Mobility.PickUpPointRepository;
using ProductRepository = IBS.DataAccess.Repository.MasterFile.ProductRepository;
using PurchaseOrderRepository = IBS.DataAccess.Repository.Mobility.PurchaseOrderRepository;
using ReceivingReportRepository = IBS.DataAccess.Repository.Mobility.ReceivingReportRepository;
using ServiceRepository = IBS.DataAccess.Repository.Mobility.ServiceRepository;
using SupplierRepository = IBS.DataAccess.Repository.Mobility.SupplierRepository;

namespace IBS.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;

        public IProductRepository Product { get; private set; }
        public ICompanyRepository Company { get; private set; }

        public INotificationRepository Notifications { get; private set; }

        #region--Mobility

        public IChartOfAccountRepository MobilityChartOfAccount { get; private set; }
        public ISalesHeaderRepository MobilitySalesHeader { get; private set; }
        public ISalesDetailRepository MobilitySalesDetail { get; private set; }
        public IFuelPurchaseRepository MobilityFuelPurchase { get; private set; }
        public ILubePurchaseHeaderRepository MobilityLubePurchaseHeader { get; private set; }
        public ILubePurchaseDetailRepository MobilityLubePurchaseDetail { get; private set; }
        public IPOSalesRepository MobilityPOSales { get; private set; }
        public IOfflineRepository MobilityOffline { get; private set; }
        public IGeneralLedgerRepository MobilityGeneralLedger { get; private set; }
        public IInventoryRepository MobilityInventory { get; private set; }
        public IStationRepository MobilityStation { get; private set; }
        public ISupplierRepository MobilitySupplier { get; private set; }
        public ICustomerRepository MobilityCustomer { get; private set; }
        public IBankAccountRepository MobilityBankAccount { get; private set; }
        public IServiceRepository MobilityService { get; private set; }
        public Mobility.IRepository.IProductRepository MobilityProduct { get; private set; }
        public Mobility.IRepository.IPickUpPointRepository MobilityPickUpPoint { get; private set; }
        public Mobility.IRepository.IEmployeeRepository MobilityEmployee { get; private set; }
        public Mobility.IRepository.IPurchaseOrderRepository MobilityPurchaseOrder { get; private set; }
        public Mobility.IRepository.IReceivingReportRepository MobilityReceivingReport { get; private set; }
        public Mobility.IRepository.ICheckVoucherRepository MobilityCheckVoucher { get; private set; }
        public Mobility.IRepository.IJournalVoucherRepository MobilityJournalVoucher { get; private set; }
        public Mobility.IRepository.IServiceInvoiceRepository MobilityServiceInvoice { get; private set; }
        public Mobility.IRepository.ICreditMemoRepository MobilityCreditMemo { get; private set; }
        public Mobility.IRepository.IDebitMemoRepository MobilityDebitMemo { get; private set; }
        public Mobility.IRepository.ICollectionReceiptRepository MobilityCollectionReceipt { get; private set; }

        public ICustomerOrderSlipRepository MobilityCustomerOrderSlip { get; private set; }

        public IDepositRepository MobilityDeposit { get; private set; }

        #endregion

        #region--Filpride

        public Filpride.IRepository.ICustomerOrderSlipRepository FilprideCustomerOrderSlip { get; private set; }
        public IDeliveryReceiptRepository FilprideDeliveryReceipt { get; private set; }
        public Filpride.IRepository.ICustomerRepository FilprideCustomer { get; private set; }
        public Filpride.IRepository.ISupplierRepository FilprideSupplier { get; private set; }
        public Filpride.IRepository.IPickUpPointRepository FilpridePickUpPoint { get; private set; }
        public IFreightRepository FilprideFreight { get; private set; }
        public IAuthorityToLoadRepository FilprideAuthorityToLoad { get; private set; }
        public Filpride.IRepository.IChartOfAccountRepository FilprideChartOfAccount { get; private set; }
        public IAuditTrailRepository FilprideAuditTrail { get; private set; }
        public Filpride.IRepository.IEmployeeRepository FilprideEmployee { get; private set; }

        #endregion

        #region AAS

        #region Accounts Receivable
        public ISalesInvoiceRepository FilprideSalesInvoice { get; private set; }

        public Filpride.IRepository.IServiceInvoiceRepository FilprideServiceInvoice { get; private set; }

        public Filpride.IRepository.ICollectionReceiptRepository FilprideCollectionReceipt { get; private set; }

        public Filpride.IRepository.IDebitMemoRepository FilprideDebitMemo { get; private set; }

        public Filpride.IRepository.ICreditMemoRepository FilprideCreditMemo { get; private set; }
        #endregion

        #region Accounts Payable
        public Filpride.IRepository.ICheckVoucherRepository FilprideCheckVoucher { get; private set; }

        public Filpride.IRepository.IJournalVoucherRepository FilprideJournalVoucher { get; private set; }

        public Filpride.IRepository.IPurchaseOrderRepository FilpridePurchaseOrder { get; private set; }

        public Filpride.IRepository.IReceivingReportRepository FilprideReceivingReport { get; private set; }
        #endregion

        #region Books and Report
        public Filpride.IRepository.IInventoryRepository FilprideInventory { get; private set; }

        public IReportRepository FilprideReport { get; private set; }
        #endregion

        #region Master File

        public Filpride.IRepository.IBankAccountRepository FilprideBankAccount { get; private set; }

        public Filpride.IRepository.IServiceRepository FilprideService { get; private set; }

        #endregion

        #endregion

        #region --Bienes

        public IPlacementRepository BienesPlacement { get; private set; }

        #endregion

        #region --MMSI

        public IMsapRepository Msap { get; private set; }
        public IServiceRequestRepository ServiceRequest { get; private set; }
        public IDispatchTicketRepository DispatchTicket { get; private set; }
        public IBillingRepository Billing { get; private set; }
        public ICollectionRepository Collection { get; private set; }
        public IMMSIReportRepository MMSIReport { get; private set; }
        public ITariffTableRepository TariffTable { get; private set; }
        public IPortRepository Port { get; private set; }
        public ITerminalRepository Terminal { get; private set; }
        public ITugboatRepository Tugboat { get; private set; }
        public IVesselRepository Vessel { get; private set; }

        #endregion

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;

            Product = new ProductRepository(_db);
            Company = new CompanyRepository(_db);
            Notifications = new NotificationRepository(_db);

            #region--Mobility

            MobilityChartOfAccount = new ChartOfAccountRepository(_db);
            MobilitySalesHeader = new SalesHeaderRepository(_db);
            MobilitySalesDetail = new SalesDetailRepository(_db);
            MobilityFuelPurchase = new FuelPurchaseRepository(_db);
            MobilityLubePurchaseHeader = new LubePurchaseHeaderRepository(_db);
            MobilityLubePurchaseDetail = new LubePurchaseDetailRepository(_db);
            MobilityPOSales = new POSalesRepository(_db);
            MobilityOffline = new OfflineRepository(_db);
            MobilityGeneralLedger = new GeneralLedgerRepository(_db);
            MobilityInventory = new InventoryRepository(_db);
            MobilityStation = new StationRepository(_db);
            MobilitySupplier = new SupplierRepository(_db);
            MobilityCustomer = new CustomerRepository(_db);
            MobilityBankAccount = new BankAccountRepository(_db);
            MobilityService = new ServiceRepository(_db);
            MobilityProduct = new Mobility.ProductRepository(_db);
            MobilityPickUpPoint = new Mobility.PickUpPointRepository(_db);
            MobilityEmployee = new Mobility.EmployeeRepository(_db);
            MobilityPurchaseOrder = new Mobility.PurchaseOrderRepository(_db);
            MobilityReceivingReport = new Mobility.ReceivingReportRepository(_db);
            MobilityCheckVoucher = new Mobility.CheckVoucherRepository(_db);
            MobilityJournalVoucher = new Mobility.JournalVoucherRepository(_db);
            MobilityCustomerOrderSlip = new Mobility.CustomerOrderSlipRepository(_db);
            MobilityServiceInvoice = new Mobility.ServiceInvoiceRepository(_db);
            MobilityCreditMemo = new Mobility.CreditMemoRepository(_db);
            MobilityDebitMemo = new Mobility.DebitMemoRepository(_db);
            MobilityCollectionReceipt = new Mobility.CollectionReceiptRepository(_db);
            MobilityDeposit = new DepositRepository(_db);

            #endregion

            #region--Filpride

            FilprideCustomerOrderSlip = new Filpride.CustomerOrderSlipRepository(_db);
            FilprideDeliveryReceipt = new DeliveryReceiptRepository(_db);
            FilprideCustomer = new Filpride.CustomerRepository(_db);
            FilprideSupplier = new Filpride.SupplierRepository(_db);
            FilpridePickUpPoint = new Filpride.PickUpPointRepository(_db);
            FilprideFreight = new FreightRepository(_db);
            FilprideAuthorityToLoad = new AuthorityToLoadRepository(_db);
            FilprideChartOfAccount = new Filpride.ChartOfAccountRepository(_db);
            FilprideAuditTrail = new AuditTrailRepository(_db);
            FilprideEmployee = new Filpride.EmployeeRepository(_db);

            #endregion

            #region AAS

            #region Accounts Receivable
            FilprideSalesInvoice = new SalesInvoiceRepository(_db);
            FilprideServiceInvoice = new Filpride.ServiceInvoiceRepository(_db);
            FilprideCollectionReceipt = new Filpride.CollectionReceiptRepository(_db);
            FilprideDebitMemo = new Filpride.DebitMemoRepository(_db);
            FilprideCreditMemo = new Filpride.CreditMemoRepository(_db);
            #endregion

            #region Accounts Payable
            FilprideCheckVoucher = new Filpride.CheckVoucherRepository(_db);
            FilprideJournalVoucher = new Filpride.JournalVoucherRepository(_db);
            FilpridePurchaseOrder = new Filpride.PurchaseOrderRepository(_db);
            FilprideReceivingReport = new Filpride.ReceivingReportRepository(_db);
            #endregion

            #region Books and Report
            FilprideInventory = new Filpride.InventoryRepository(_db);
            FilprideReport = new ReportRepository(_db);
            #endregion

            #region Master File

            FilprideBankAccount = new Filpride.BankAccountRepository(_db);
            FilprideService = new Filpride.ServiceRepository(_db);

            #endregion

            #endregion

            #region --MMSI

            Msap = new MsapRepository(_db);
            ServiceRequest = new ServiceRequestRepository(_db);
            DispatchTicket = new DispatchTicketRepository(_db);
            Billing = new BillingRepository(_db);
            Collection = new CollectionRepository(_db);
            MMSIReport = new MMSIReportRepository(_db);
            TariffTable = new TariffTableRepository(_db);
            Terminal = new TerminalRepository(_db);
            Tugboat = new TugboatRepository(_db);

            #endregion

            #region --Bienes

            BienesPlacement = new PlacementRepository(_db);

            #endregion
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public void Dispose() => _db.Dispose();

        #region--Mobility

        public async Task<List<SelectListItem>> GetMobilityStationListAsyncByCode(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityStations
                .OrderBy(s => s.StationCode)
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.StationCode,
                    Text = s.StationCode + " " + s.StationName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMobilityStationListWithCustomersAsyncByCode(List<MobilityCustomer> mobilityCustomers, CancellationToken cancellationToken = default)
        {
            var customerStationCodes = mobilityCustomers
               .Select(mc => mc.StationCode)
               .Distinct() // Optional: To ensure no duplicate StationCodes
               .ToList();

            List<SelectListItem> selectListItem = await _db.MobilityStations
                .Where(s => s.IsActive)
                .Where(s => customerStationCodes.Contains(s.StationCode)) // Filter based on StationCode
                .OrderBy(s => s.StationId)
                .Select(s => new SelectListItem
                {
                    Value = s.StationCode,
                    Text = s.StationCode + " " + s.StationName
                })
                .ToListAsync(cancellationToken);

            return selectListItem;
        }

        public async Task<List<SelectListItem>> GetMobilityStationListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityStations
                .OrderBy(s => s.StationId)
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.StationId.ToString(),
                    Text = s.StationCode + " " + s.StationName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMobilityCustomerListAsyncByCodeName(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityCustomers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerCodeName,
                    Text = c.CustomerName.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMobilityCustomerListAsyncByCode(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityCustomers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerCodeName,
                    Text = c.CustomerName.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMobilityCustomerListAsyncById(string stationCodeClaims, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityCustomers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.IsActive)
                .Where(c => c.StationCode == stationCodeClaims)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMobilityCustomerListAsyncByIdAll(string stationCodeClaims, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityCustomers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMobilitySupplierListAsyncById(string stationCodeClaims, CancellationToken cancellationToken = default)
        {
            return await _db.MobilitySuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.StationCode == stationCodeClaims)
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GetMobilityStationNameAsync(string stationCodeClaims, CancellationToken cancellationToken)
        {
            string stationString;

            var station = await _db.MobilityStations
                .Where(station => station.StationCode == stationCodeClaims)
                .FirstOrDefaultAsync(cancellationToken);

            string stationName = station?.StationName ?? "Unknown Station";
            string fullStationName = stationName + " STATION";
            stationString = fullStationName;

            return stationString;
        }

        public async Task<List<SelectListItem>> GetMobilityProductListAsyncByCode(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityProducts
                .OrderBy(p => p.ProductId)
                .Where(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Value = p.ProductCode,
                    Text = p.ProductCode + " " + p.ProductName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMobilityProductListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityProducts
                .OrderBy(p => p.ProductId)
                .Where(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = p.ProductCode + " " + p.ProductName
                })
                .ToListAsync(cancellationToken);
        }

        #endregion

        #region--Filpride

        // Make the function generic
        Expression<Func<T, bool>> GetCompanyFilter<T>(string companyName) where T : class
        {
            // Use reflection or property pattern matching to dynamically access properties
            var param = Expression.Parameter(typeof(T), "x");

            // Build the appropriate expression based on the company name
            Expression propertyAccess = companyName switch
            {
                nameof(Filpride) => Expression.Property(param, "IsFilpride"),
                nameof(Mobility) => Expression.Property(param, "IsMobility"),
                nameof(Bienes) => Expression.Property(param, "IsBienes"),
                nameof(MMSI) => Expression.Property(param, "IsMMSI"),
                _ => Expression.Constant(false)
            };

            return Expression.Lambda<Func<T, bool>>(propertyAccess, param);
        }

        public async Task<List<SelectListItem>> GetFilprideCustomerListAsyncById(string company, CancellationToken cancellationToken = default)
        {

            return await _db.FilprideCustomers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.IsActive)
                .Where(GetCompanyFilter<FilprideCustomer>(company))
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMobilityCustomerListAsync(string stationCodeClaims, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityCustomers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.IsActive && c.StationCode == stationCodeClaims)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetFilprideSupplierListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideSuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive)
                .Where(GetCompanyFilter<FilprideSupplier>(company))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetFilprideTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideSuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.Category == "Trade")
                .Where(GetCompanyFilter<FilprideSupplier>(company))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetFilprideNonTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideSuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.Category == "Non-Trade")
                .Where(GetCompanyFilter<FilprideSupplier>(company))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetFilprideCommissioneeListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideSuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.Category == "Commissionee")
                .Where(GetCompanyFilter<FilprideSupplier>(company))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetFilprideHaulerListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideSuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.Company == company && s.Category == "Hauler")
                .Where(GetCompanyFilter<FilprideSupplier>(company))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetFilprideBankAccountListById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideBankAccounts
                .Where(GetCompanyFilter<FilprideBankAccount>(company))
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetFilprideEmployeeListById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideEmployees
                .Where(e => e.IsActive && e.Company == company)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetDistinctFilpridePickupPointListById(string companyClaims, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePickUpPoints
                .Where(GetCompanyFilter<FilpridePickUpPoint>(companyClaims))
                .GroupBy(p => p.Depot)
                .OrderBy(g => g.Key)
                .Select(g => new SelectListItem
                {
                    Value = g.First().PickUpPointId.ToString(),
                    Text = g.Key // g.Key is the Depot name
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetFilprideServiceListById(string companyClaims, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideServices
                .OrderBy(s => s.ServiceId)
                .Where(GetCompanyFilter<FilprideService>(companyClaims))
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);
        }

        #endregion

        public async Task<List<SelectListItem>> GetProductListAsyncByCode(CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .OrderBy(p => p.ProductId)
                .Where(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Value = p.ProductCode,
                    Text = p.ProductCode + " " + p.ProductName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetProductListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .OrderBy(p => p.ProductId)
                .Where(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = p.ProductCode + " " + p.ProductName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCashierListAsyncByUsernameAsync(CancellationToken cancellationToken = default)
        {
            return await _db.ApplicationUsers
                .OrderBy(p => p.Id)
                .Where(p => p.Department == SD.Department_StationCashier)
                .Select(p => new SelectListItem
                {
                    Value = p.UserName!.ToString(),
                    Text = p.UserName.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCashierListAsyncByStationAsync(CancellationToken cancellationToken = default)
        {
            return await _db.ApplicationUsers
                .OrderBy(p => p.Id)
                .Where(p => p.Department == SD.Department_StationCashier)
                .Select(p => new SelectListItem
                {
                    Value = p.StationAccess!.ToString(),
                    Text = p.UserName!.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetChartOfAccountListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetChartOfAccountListAsyncByNo(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetChartOfAccountListAsyncByAccountTitle(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCompanyListAsyncByName(CancellationToken cancellationToken = default)
        {
            return await _db.Companies
                .OrderBy(c => c.CompanyCode)
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CompanyName,
                    Text = c.CompanyCode + " " + c.CompanyName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCompanyListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.Companies
                .OrderBy(c => c.CompanyCode)
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CompanyId.ToString(),
                    Text = c.CompanyCode + " " + c.CompanyName
                })
                .ToListAsync(cancellationToken);
        }
    }
}
