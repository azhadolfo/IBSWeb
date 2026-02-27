using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class JvEditAccrualViewModel
    {
        public int JvId { get; set; }

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

        public List<JvEditAccrualDetailViewModel> Details { get; set; } = [];
    }

    public class JvEditAccrualDetailViewModel
    {
        public string AccountNo { get; set; } = string.Empty;
        public string AccountTitle { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
