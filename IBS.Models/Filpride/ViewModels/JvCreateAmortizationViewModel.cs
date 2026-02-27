using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class JvCreateAmortizationViewModel
    {
        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public DateOnly TransactionDate { get; set; }

        public DateTime MinDate { get; set; }

        public string? References { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a check voucher.")]
        public int CvId { get; set; }

        public List<SelectListItem>? CvList { get; set; }

        public string? CrNo { get; set; }

        [Required]
        [StringLength(200)]
        public string Reason { get; set; } = string.Empty;

        public List<JvCreateAmortizationDetailViewModel> Details { get; set; } = [];

        [Range(1, int.MaxValue, ErrorMessage = "Number of months must be greater than zero.")]
        public int NumberOfMonths { get; set; }

        [Required]
        public string SelectedExpenseAccount { get; set; } = string.Empty;

        [Required]
        public string SelectedPrepaidAccount { get; set; } = string.Empty;

        public List<SelectListItem>? PrepaidExpenseAccounts { get; set; }
    }

    public class JvCreateAmortizationDetailViewModel
    {
        public string AccountNo { get; set; } = string.Empty;
        public string AccountTitle { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
