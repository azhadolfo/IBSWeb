using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.ViewModels
{
    public class ReceivingReportViewModel
    {
        public int? ReceivingReportId { get; set; }

        public DateOnly Date { get; set; }

        public string Driver { get; set; }

        public string PlateNo { get; set; }

        public string Remarks { get; set; }

        #region DR FILPRIDE

        public int DeliveryReceiptId { get; set; }

        public List<SelectListItem>? DrList { get; set; }

        public decimal InvoiceQuantity { get; set; }

        public string Product { get; set; }

        #endregion DR FILPRIDE

        public decimal ReceivedQuantity { get; set; }

        public string? CurrentUser { get; set; }
    }
}