using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.ViewModels
{
    public class ManualEntryViewModel
    {
        public List<SelectListItem>? OfflineList { get; set; }

        public int SelectedOfflineId { get; set; }

        public double Closing { get; set; }

        public double Opening { get; set; } //Mapped via selected offline list - readonly

        public string AffectedDSRNo { get; set; }

    }
}
