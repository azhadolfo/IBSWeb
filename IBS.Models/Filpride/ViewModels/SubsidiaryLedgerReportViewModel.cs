using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class SubsidiaryLedgerReportViewModel
    {
        public DateOnly MonthDate { get; set; }

        public List<SelectListItem>? ChartOfAccounts { get; set; }

        public string? AccountNo { get; set; }
    }
}
