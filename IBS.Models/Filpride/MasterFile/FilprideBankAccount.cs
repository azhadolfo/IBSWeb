using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideBankAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BankAccountId { get; set; }

        [StringLength(10)]
        public string Bank { get; set; } = null!;

        [StringLength(200)]
        public string Branch { get; set; } = null!;

        [StringLength(20)]
        [Display(Name = "Account No")]
        public string AccountNo { get; set; } = null!;

        [StringLength(200)]
        [Display(Name = "Account Name")]
        public string AccountName { get; set; } = null!;

        [Display(Name = "Created By")]
        [StringLength(100)]
        public string? CreatedBy { get; set; } = "";

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"));

    }
}
