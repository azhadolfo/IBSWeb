namespace IBS.Models.Filpride.ViewModels
{
    public class DashboardCountViewModel
    {
        public int SupplierAppointmentCount { get; set; }
        public int HaulerAppointmentCount { get; set; }
        public int ATLBookingCount { get; set; }
        public int OMApprovalCOSCount { get; set; }
        public int OMApprovalDRCount { get; set; }
        public int OMApprovalPOCount { get; set; }
        public int CNCApprovalCount { get; set; }
        public int FMApprovalCount { get; set; }
        public int DRCount { get; set; }
        public int InTransitCount { get; set; }
        public int ForInvoiceCount { get; set; }
        public int RecordLiftingDateCount { get; set; }
        public int MobilityStationUnservePO { get; set; }
        public int MMSIServiceRequestForPosting { get; set; }
        public int MMSIDispatchTicketForTariff { get; set; }
        public int MMSIDispatchTicketForApproval { get; set; }
        public int MMSIDispatchTicketForBilling { get; set; }
        public int MMSIBillingForCollection { get; set; }
    }
}
