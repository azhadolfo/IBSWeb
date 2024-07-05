using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Mobility.ViewModels
{
    public class AdjustReportViewModel
    {
        public List<SelectListItem>? OfflineList { get; set; }

        public int SelectedOfflineId { get; set; }

        [Display(Name = "First Closing")]
        public decimal FirstDsrClosingBefore { get; set; }

        [Display(Name = "First Opening")]
        public decimal FirstDsrOpeningBefore { get; set; }

        [Display(Name = "Second Closing")]
        public decimal SecondDsrClosingBefore { get; set; }

        [Display(Name = "Second Opening")]
        public decimal SecondDsrOpeningBefore { get; set; }

        [Display(Name = "First Closing")]
        public decimal FirstDsrClosingAfter { get; set; }

        [Display(Name = "First Opening")]
        public decimal FirstDsrOpeningAfter { get; set; }

        [Display(Name = "Second Closing")]
        public decimal SecondDsrClosingAfter { get; set; }

        [Display(Name = "Second Opening")]
        public decimal SecondDsrOpeningAfter { get; set; }

        public string AffectedDSRNo { get; set; }
    }
}