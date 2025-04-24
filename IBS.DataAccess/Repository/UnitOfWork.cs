using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Bienes;
using IBS.DataAccess.Repository.Bienes.IRepository;
using IBS.DataAccess.Repository.Filpride;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MasterFile;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.DataAccess.Repository.Mobility;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BankAccountRepository = IBS.DataAccess.Repository.Filpride.BankAccountRepository;
using IBankAccountRepository = IBS.DataAccess.Repository.Filpride.IRepository.IBankAccountRepository;

namespace IBS.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;

        public MasterFile.IRepository.IProductRepository Product { get; private set; }
        public ICompanyRepository Company { get; private set; }

        public INotificationRepository Notifications { get; private set; }

        #region--Mobility

        public Mobility.IRepository.IChartOfAccountRepository MobilityChartOfAccount { get; private set; }
        public ISalesHeaderRepository MobilitySalesHeader { get; private set; }
        public ISalesDetailRepository MobilitySalesDetail { get; private set; }
        public IFuelPurchaseRepository MobilityFuelPurchase { get; private set; }
        public ILubePurchaseHeaderRepository MobilityLubePurchaseHeader { get; private set; }
        public ILubePurchaseDetailRepository MobilityLubePurchaseDetail { get; private set; }
        public IPOSalesRepository MobilityPOSales { get; private set; }
        public IOfflineRepository MobilityOffline { get; private set; }
        public IGeneralLedgerRepository MobilityGeneralLedger { get; private set; }
        public Mobility.IRepository.IInventoryRepository MobilityInventory { get; private set; }
        public IStationRepository MobilityStation { get; private set; }
        public Mobility.IRepository.ISupplierRepository MobilitySupplier { get; private set; }
        public Mobility.IRepository.ICustomerRepository MobilityCustomer { get; private set; }
        public Mobility.IRepository.IBankAccountRepository MobilityBankAccount { get; private set; }
        public Mobility.IRepository.IServiceRepository MobilityService { get; private set; }
        public Mobility.IRepository.IProductRepository MobilityProduct { get; private set; }
        public Mobility.IRepository.IPickUpPointRepository MobilityPickUpPoint { get; private set; }
        public Mobility.IRepository.IEmployeeRepository MobilityEmployee { get; private set; }
        public Mobility.IRepository.IPurchaseOrderRepository MobilityPurchaseOrder { get; private set; }
        public Mobility.IRepository.IReceivingReportRepository MobilityReceivingReport { get; private set; }
        public Mobility.IRepository.ICheckVoucherRepository MobilityCheckVoucher { get; private set; }
        public Mobility.IRepository.IServiceInvoiceRepository MobilityServiceInvoice { get; private set; }
        public Mobility.IRepository.ICreditMemoRepository MobilityCreditMemo { get; private set; }
        public Mobility.IRepository.IDebitMemoRepository MobilityDebitMemo { get; private set; }
        public Mobility.IRepository.ICollectionReceiptRepository MobilityCollectionReceipt { get; private set; }

        public Mobility.IRepository.ICustomerOrderSlipRepository MobilityCustomerOrderSlip { get; private set; }

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

        public IJournalVoucherRepository FilprideJournalVoucher { get; private set; }

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

        public Bienes.IRepository.IBankAccountRepository BienesBankAccount { get; private set; }

        public IPlacementRepository BienesPlacement { get; private set; }

        #endregion

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;

            Product = new MasterFile.ProductRepository(_db);
            Company = new CompanyRepository(_db);
            Notifications = new NotificationRepository(_db);

            #region--Mobility

            MobilityChartOfAccount = new Mobility.ChartOfAccountRepository(_db);
            MobilitySalesHeader = new SalesHeaderRepository(_db);
            MobilitySalesDetail = new SalesDetailRepository(_db);
            MobilityFuelPurchase = new FuelPurchaseRepository(_db);
            MobilityLubePurchaseHeader = new LubePurchaseHeaderRepository(_db);
            MobilityLubePurchaseDetail = new LubePurchaseDetailRepository(_db);
            MobilityPOSales = new POSalesRepository(_db);
            MobilityOffline = new OfflineRepository(_db);
            MobilityGeneralLedger = new GeneralLedgerRepository(_db);
            MobilityInventory = new Mobility.InventoryRepository(_db);
            MobilityStation = new StationRepository(_db);
            MobilitySupplier = new Mobility.SupplierRepository(_db);
            MobilityCustomer = new Mobility.CustomerRepository(_db);
            MobilityBankAccount = new Mobility.BankAccountRepository(_db);
            MobilityService = new Mobility.ServiceRepository(_db);
            MobilityProduct = new Mobility.ProductRepository(_db);
            MobilityPickUpPoint = new Mobility.PickUpPointRepository(_db);
            MobilityEmployee = new Mobility.EmployeeRepository(_db);
            MobilityPurchaseOrder = new Mobility.PurchaseOrderRepository(_db);
            MobilityReceivingReport = new Mobility.ReceivingReportRepository(_db);
            MobilityCheckVoucher = new Mobility.CheckVoucherRepository(_db);
            MobilityCustomerOrderSlip = new Mobility.CustomerOrderSlipRepository(_db);
            MobilityServiceInvoice = new Mobility.ServiceInvoiceRepository(_db);
            MobilityCreditMemo = new Mobility.CreditMemoRepository(_db);
            MobilityDebitMemo = new Mobility.DebitMemoRepository(_db);
            MobilityCollectionReceipt = new Mobility.CollectionReceiptRepository(_db);

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
            FilprideJournalVoucher = new JournalVoucherRepository(_db);
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

            #region --Bienes

            BienesBankAccount = new Bienes.BankAccountRepository(_db);
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

        #endregion

        #region--Filpride

        public async Task<List<SelectListItem>> GetFilprideCustomerListAsync(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.IsActive && (company == nameof(Filpride) ? c.IsFilpride : c.IsMobility))
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
                .Where(s => s.IsActive && (company == nameof(Filpride) ? s.IsFilpride : s.IsMobility))
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
                .Where(s => s.IsActive && s.Category == "Commissionee" && (company == nameof(Filpride) ? s.IsFilpride : s.IsMobility))
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
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
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
                    Value = p.UserName.ToString(),
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
                    Value = p.StationAccess.ToString(),
                    Text = p.UserName.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetChartOfAccountListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityChartOfAccounts
                .OrderBy(c => c.AccountId)
                .Where(c => c.Level == 4)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountId.ToString(),
                    Text = c.AccountNumber + " " + c.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetChartOfAccountListAsyncByNo(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                .Where(c => c.Level == 4)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountNumber,
                    Text = c.AccountNumber + " " + c.AccountName
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
    }
}
