using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class GeneralLedgerReportViewModel
    {
        [Display(Name = "Date From")]
        public DateOnly DateFrom { get; set; }

        [Display(Name = "Date To")]
        public DateOnly DateTo { get; set; }

        public List<SelectListItem>? ChartOfAccounts { get; set; }

        public string? AccountNo { get; set; }
    }
}
