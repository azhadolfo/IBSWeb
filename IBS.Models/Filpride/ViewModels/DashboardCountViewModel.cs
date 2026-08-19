namespace IBS.Models.Filpride.ViewModels
{
    public class DashboardCountViewModel
    {
        public int MarketingApprovalCount { get; set; }
        public int SupplierAppointmentCount { get; set; }
        public int HaulerAppointmentCount { get; set; }
        public int ATLBookingCount { get; set; }
        public int OMApprovalCOSCount { get; set; }
        public int OMApprovalDRCount { get; set; }
        public int OMApprovalPOCount { get; set; }
        public int CNCApprovalCount { get; set; }
        public int FMApprovalCount { get; set; }
        public int FMApprovalDMCount { get; set; }
        public int FMApprovalCMCount { get; set; }
        public int DRCount { get; set; }
        public int InTransitCount { get; set; }
        public int ForInvoiceCount { get; set; }
        public int RecordLiftingDateCount { get; set; }
        public int RecordSupplierDetails { get; set; }

        // Accounting - For Approval Counts
        public int JournalVoucherForApprovalCount { get; set; }
        public int CheckVoucherNonTradeInvoiceForApprovalCount { get; set; }
        public int CheckVoucherNonTradePayrollInvoiceForApprovalCount { get; set; }

        // Sidebar data
        public List<PendingApprovalItem> MySubmissions { get; set; } = new();
        public List<PendingApprovalItem> PendingMyApproval { get; set; } = new();
        public string UserFullName { get; set; } = string.Empty;
        public bool ShowPriority { get; set; }
    }

    public class PendingApprovalItem
    {
        public int Id { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Area { get; set; } = "Filpride";
        public string Controller { get; set; } = string.Empty;
        public string FilterType { get; set; } = string.Empty;
        public string DisplayStatus { get; set; } = string.Empty;
        public string SidebarUrl { get; set; } = "#";
        public DateTime CreatedDate { get; set; }
    }
}
