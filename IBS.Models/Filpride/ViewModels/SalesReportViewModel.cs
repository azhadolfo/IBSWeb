using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Integrated;

namespace IBS.Models.Filpride.ViewModels
{
    public class SalesReportViewModel
    {
        public FilprideSalesInvoice? SalesInvoice { get; set; }
        public FilprideDeliveryReceipt DeliveryReceipt { get; set; }

        public string SalesInvoiceNo => SalesInvoice?.SalesInvoiceNo ?? " ";
    }
}
