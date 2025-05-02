using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;

namespace IBS.Models.Bienes
{
    public class BienesPlacement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlacementId { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string ControlNumber { get; set; }

        public int CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public Company Company { get; set; }

        public int BankId { get; set; }

        [ForeignKey(nameof(BankId))]
        public FilprideBankAccount? BankAccount { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Bank { get; set; }

        [Column(TypeName = "varchar(100)")]

        public string Branch { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string AccountName { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string Class { get; set; } = "STP";

        public int SettlementAccountId { get; set; }

        [ForeignKey(nameof(SettlementAccountId))]
        public FilprideBankAccount? SettlementAccount { get; set; }

        [Column(TypeName = "date")]
        public DateOnly DateFrom { get; set; }

        [Column(TypeName = "date")]
        public DateOnly DateTo { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string Remarks { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string ChequeNumber { get; set; } = string.Empty;

        [Column(TypeName = "varchar(100)")]
        public string CVNo { get; set; } = string.Empty;

        [Column(TypeName = "varchar(5)")]
        public string Disposition { get; set; } = "O";

        [Column(TypeName = "numeric(18,2)")]
        public decimal PrincipalAmount { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? PrincipalDisposition { get; set; }

        public PlacementType PlacementType { get; set; }

        public int NumberOfYears { get; set; }

        [Column(TypeName = "numeric(13,10)")]
        public decimal InterestRate { get; set; }

        public bool HasEWT { get; set; }

        [Column(TypeName = "numeric(7,4)")]
        public decimal EWTRate { get; set; }

        public bool HasTrustFee { get; set; }

        [Column(TypeName = "numeric(11,8)")]
        public decimal TrustFeeRate { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal InterestDeposited { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? InterestDepositedTo { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? InterestDepositedDate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? FrequencyOfPayment { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [Column(TypeName = "varchar(100)")]
        public string? PostedBy { get; set; }

        public DateTime? PostedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? TerminatedBy { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? TerminatedDate { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string? TerminationRemarks { get; set; }

        public bool IsLocked { get; set; }

        public DateTime? LockedDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? InterestStatus { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string TDAccountNumber { get; set; }

        public bool IsPosted { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? BatchNumber { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Status { get; set; } = nameof(PlacementStatus.Unposted);

        [Column(TypeName = "varchar(50)")]
        public string EditedBy { get; set; } = string.Empty;

        public DateTime? EditedDate { get; set; }

        public bool IsRolled { get; set; }

        public int? RolledFromId { get; set; }

        [ForeignKey(nameof(RolledFromId))]
        public BienesPlacement RolledFrom { get; set; }
    }
}
