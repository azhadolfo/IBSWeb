using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;

namespace IBS.Models.Filpride.AccountsReceivable
{
    public class FilprideProvisionalReceipt : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(20)]
        public string SeriesNumber { get; set; } = string.Empty;

        public DateOnly TransactionDate { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public FilprideSupplier Supplier { get; set; } = null!;

        [StringLength(255)]
        public string ReferenceNo { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Remarks { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CashAmount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CheckAmount { get; set; }

        public DateOnly? CheckDate { get; set; }

        [StringLength(255)]
        public string? CheckNo { get; set; }

        [StringLength(255)]
        public string? CheckBank { get; set; }

        [StringLength(255)]
        public string? CheckBranch { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal ManagersCheckAmount { get; set; }

        public DateOnly? ManagersCheckDate { get; set; }

        [StringLength(255)]
        public string? ManagersCheckNo { get; set; }

        [StringLength(255)]
        public string? ManagersCheckBank { get; set; }

        [StringLength(255)]
        public string? ManagersCheckBranch { get; set; }

        public int? BankId { get; set; }

        public FilprideBankAccount? BankAccount { get; set; }

        [StringLength(255)]
        public string? BankAccountNo { get; set; }

        [StringLength(255)]
        public string? BankAccountName { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal EWT { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal WVAT { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Total { get; set; }

        public bool IsPrinted { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        [StringLength(20)]
        public string Type { get; set; } = string.Empty;

        public DateOnly? DepositedDate { get; set; }

        public DateOnly? ClearedDate { get; set; }

        [StringLength(255)]
        public string? BatchNumber { get; set; }
    }
}
