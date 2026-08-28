using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IReportRepository : IRepository<FilprideGeneralLedgerBook>
    {
        Task<List<FilprideReceivingReport>> GetReceivingReportAsync(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering, CancellationToken cancellationToken = default);

        List<FilprideInventory> GetInventoryBooks(DateOnly dateFrom, DateOnly dateTo);

        Task<List<FilprideGeneralLedgerBook>> GetGeneralLedgerBooks(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default);

        Task<List<FilprideAuditTrail>> GetAuditTrails(DateOnly dateFrom, DateOnly dateTo);

        Task<List<FilprideCustomerOrderSlip>> GetCosUnservedVolume(DateOnly dateFrom, DateOnly dateTo);

        public Task<List<SalesReportViewModel>> GetSalesReport(DateOnly dateFrom, DateOnly dateTo, List<int>? commissioneeIds = null, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default);

        public Task<List<FilprideSalesInvoice>> GetSalesInvoiceReport(DateOnly dateFrom, DateOnly dateTo, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default);

        public Task<List<FilpridePurchaseOrder>> GetPurchaseOrderReport(DateOnly dateFrom, DateOnly dateTo, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default);

        public Task<List<FilprideCheckVoucherHeader>> GetClearedDisbursementReport(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default);

        public Task<List<FilprideReceivingReport>> GetPurchaseReport(DateOnly dateFrom, DateOnly dateTo, List<int>? customerIds = null, List<int>? commissioneeIds = null, string dateSelectionType = "RRDate", string statusFilter = "ValidOnly", CancellationToken cancellationToken = default);

        public Task<List<FilprideDeliveryReceipt>> GetGrossMarginReport( DateOnly dateFrom, DateOnly dateTo, List<int>? customers = null, List<int>? commissionee = null, CancellationToken cancellationToken = default);
        public Task<List<FilprideCollectionReceipt>> GetCollectionReceiptReport(DateOnly dateFrom, DateOnly dateTo, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default);

        public Task<List<FilprideReceivingReport>> GetTradePayableReport(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default);

        public Task<List<FilprideDeliveryReceipt>> GetHaulerPayableReport(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default);

        public Task<List<FilprideServiceInvoice>> GetServiceInvoiceReport(DateOnly dateFrom, DateOnly dateTo, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default);

        public Task<List<FilpridePurchaseOrder>> GetApReport(DateOnly monthYear, CancellationToken cancellationToken = default);

        public Task<List<FilprideSalesInvoice>> GetARPerCustomerReport(DateOnly dateFrom, DateOnly dateTo, List<int>? customerIds = null, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default);

        public Task<List<FilprideJournalVoucherDetail>> GetJournalVoucherReport(DateOnly dateFrom, DateOnly dateTo, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default);

        public Task<List<FilprideCustomerOrderSlip>> GetCustomerOrderSlipReport(DateOnly dateFrom, DateOnly dateTo, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default);
    }
}
