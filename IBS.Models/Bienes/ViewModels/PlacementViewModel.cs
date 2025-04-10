using System.ComponentModel.DataAnnotations;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Bienes.ViewModels
{
    public class PlacementViewModel
    {
        public int PlacementId { get; set; }

        public int CompanyId { get; set; }

        public List<SelectListItem>? Companies { get; set; }

        public int BankId { get; set; }

        public List<SelectListItem>? BankAccounts { get; set; }

        public string Bank { get; set; } = string.Empty;

        public string Branch { get; set; } = string.Empty;

        [Required]
        public string TDAccountNumber { get; set; }

        [Required]
        public string AccountName { get; set; }

        [Required]
        public string SettlementAccountNumber { get; set; }

        [Required]
        public DateOnly FromDate { get; set; }

        [Required]
        public DateOnly ToDate { get; set; }

        public int Term { get; set; }

        [Required]
        public string Remarks { get; set; }

        public string ChequeNumber { get; set; } = string.Empty;

        public string CVNo { get; set; } = string.Empty;

        [Required]
        public decimal PrincipalAmount { get; set; }

        public decimal MaturityValue { get; set; }

        public string PrincipalDisposition { get; set; } = string.Empty;

        [Required]
        public PlacementType PlacementType { get; set; }

        public int NumberOfYears { get; set; }

        public decimal EarnedGross { get; set; }
        public decimal Net { get; set; }

        [Required]
        public decimal InterestRate { get; set; }

        public bool HasEwt { get; set; }

        public decimal EWTRate { get; set; }

        public decimal EWTAmount { get; set; }

        public bool HasTrustFee { get; set; }
        public decimal TrustFeeRate { get; set; }

        public decimal TrustFeeAmount { get; set; }

        public string? FrequencyOfPayment { get; set; }

    }
}
