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

        [Required]
        public int CvId { get; set; }

        public List<SelectListItem>? CvList { get; set; }

        [Required]
        [StringLength(200)]
        public string Particulars { get; set; } = string.Empty;

        public string? CrNo { get; set; }

        [Required]
        [StringLength(200)]
        public string Reason { get; set; }

        public List<JvCreateAccrualDetailViewModel> Details { get; set; }

        public int NumberOfMonths { get; set; }

        public string SelectedExpenseAccount { get; set; }
    }

    public class JvCreateAmortizationDetailViewModel
    {
        public string AccountNo { get; set; } = string.Empty;
        public string AccountTitle { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
