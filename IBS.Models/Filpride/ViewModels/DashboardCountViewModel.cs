namespace IBS.Models.Filpride.ViewModels
{
    public class DashboardCountViewModel
    {
        public int SupplierAppointmentCount { get; set; }
        public int HaulerAppointmentCount { get; set; }
        public int ATLBookingCount { get; set; }
        public int OMApprovalCOSCount { get; set; }
        public int OMApprovalDRCount { get; set; }
        public int FMApprovalCount { get; set; }
        public int DRCount { get; set; }
        public int InTransitCount { get; set; }
        public int InvoiceCount { get; set; }
    }
}
