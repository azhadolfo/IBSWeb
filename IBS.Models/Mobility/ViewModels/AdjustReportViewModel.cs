using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.ViewModels
{
    public class AdjustReportViewModel
    {
        public List<SelectListItem>? OfflineList { get; set; }

        public int SelectedOfflineId { get; set; }

        public decimal Closing { get; set; }

        public decimal Opening { get; set; } //Mapped via selected offline list - readonly

        public string AffectedDSRNo { get; set; }

    }
}
