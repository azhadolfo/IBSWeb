using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MasterFile;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.DataAccess.Repository.Mobility;
using IBS.DataAccess.Repository.Mobility.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;

        public IProductRepository Product { get; private set; }
        public ICompanyRepository Company { get; private set; }

        public IChartOfAccountRepository ChartOfAccount { get; private set; }

        #region--Mobility

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
        public Mobility.IRepository.IPurchaseOrderRepository MobilityPurchaseOrder { get; private set; }
        public Mobility.IRepository.IReceivingReportRepository MobilityReceivingReport { get; private set; }

        #endregion

        #region--Filpride

        public Filpride.IRepository.IPurchaseOrderRepository FilpridePurchaseOrder { get; private set; }
        public Filpride.IRepository.IReceivingReportRepository FilprideReceivingReport { get; private set; }
        public ICustomerOrderSlipRepository FilprideCustomerOrderSlip { get; private set; }
        public IDeliveryReceiptRepository FilprideDeliveryReceipt { get; private set; }
        public ICustomerRepository FilprideCustomer { get; private set; }
        public ISupplierRepository FilprideSupplier { get; private set; }
        public IHaulerRepository FilprideHauler { get; private set; }

        #endregion

        #region AAS

        #region Accounts Receivable
        public ISalesInvoiceRepository FilprideSalesInvoice { get; private set; }

        public IServiceInvoiceRepository FilprideServiceInvoice { get; private set; }

        public ICollectionReceiptRepository FilprideCollectionReceipt { get; private set; }

        public IDebitMemoRepository FilprideDebitMemo { get; private set; }

        public ICreditMemoRepository FilprideCreditMemo { get; private set; }
        #endregion

        #region Accounts Payable
        public ICheckVoucherRepository FilprideCheckVoucher { get; private set; }

        public IJournalVoucherRepository FilprideJournalVoucher { get; private set; }

        public IPurchaseOrderRepo FilpridePurchaseOrderRepo { get; private set; }

        public IReceivingReportRepo FilprideReceivingReportRepo { get; private set; }
        #endregion

        #region Books and Report
        public Filpride.IRepository.IInventoryRepository FilprideInventory { get; private set; }

        public IReportRepository FilprideReport { get; private set; }
        #endregion

        #region Master File

        public IBankAccountRepository FilprideBankAccount { get; private set; }

        #endregion

        #endregion

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;

            Product = new ProductRepository(_db);
            Company = new CompanyRepository(_db);
            ChartOfAccount = new ChartOfAccountRepository(_db);

            #region--Mobility

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
            MobilityPurchaseOrder = new Mobility.PurchaseOrderRepository(_db);
            MobilityReceivingReport = new Mobility.ReceivingReportRepository(_db);

            #endregion

            #region--Filpride

            FilpridePurchaseOrder = new Filpride.PurchaseOrderRepository(_db);
            FilprideReceivingReport = new Filpride.ReceivingReportRepository(_db);
            FilprideCustomerOrderSlip = new CustomerOrderSlipRepository(_db);
            FilprideDeliveryReceipt = new DeliveryReceiptRepository(_db);
            FilprideCustomer = new CustomerRepository(_db);
            FilprideSupplier = new SupplierRepository(_db);
            FilprideHauler = new HaulerRepository(_db);

            #endregion

            #region AAS

            #region Accounts Receivable
            FilprideSalesInvoice = new SalesInvoiceRepository(_db);
            FilprideServiceInvoice = new ServiceInvoiceRepository(_db);
            FilprideCollectionReceipt = new CollectionReceiptRepository(_db);
            FilprideDebitMemo = new DebitMemoRepository(_db);
            FilprideCreditMemo = new CreditMemoRepository(_db);
            #endregion

            #region Accounts Payable
            FilprideCheckVoucher = new CheckVoucherRepository(_db);
            FilprideJournalVoucher = new JournalVoucherRepository(_db);
            FilpridePurchaseOrderRepo = new PurchaseOrderRepo(_db);
            FilprideReceivingReportRepo = new ReceivingReportRepo(_db);
            #endregion

            #region Books and Report
            FilprideInventory = new Filpride.InventoryRepository(_db);
            FilprideReport = new ReportRepository(_db);
            FilprideBankAccount = new BankAccountRepository(_db);
            #endregion

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
                .OrderBy(s => s.StationId)
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.StationCode,
                    Text = s.StationCode + " " + s.StationName
                })
                .ToListAsync(cancellationToken);
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
                    Text = c.CustomerCode + " " + c.CustomerCodeName
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
                    Value = c.CustomerCode,
                    Text = c.CustomerCode + " " + c.CustomerCodeName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMobilitySupplierListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.MobilitySuppliers
                .OrderBy(s => s.SupplierId)
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        #endregion

        #region--Filpride

        public async Task<List<SelectListItem>> GetFilprideCustomerListAsync(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetFilprideSupplierListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideSuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive)
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

        public async Task<List<SelectListItem>> GetChartOfAccountListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.ChartOfAccounts
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
            return await _db.ChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                .Where(c => c.Level == 4)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountNumber,
                    Text = c.AccountNumber + " " + c.AccountName
                })
                .ToListAsync(cancellationToken);
        }
    }
}