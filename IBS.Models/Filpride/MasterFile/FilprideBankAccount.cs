using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility.Helpers;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideBankAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BankAccountId { get; set; }

        [StringLength(10)]
        public string Bank { get; set; }

        [StringLength(200)]
        public string Branch { get; set; }

        [StringLength(20)]
        [Display(Name = "Account No")]
        public string AccountNo { get; set; }

        [StringLength(200)]
        [Display(Name = "Account Name")]
        public string AccountName { get; set; }

        [Display(Name = "Created By")]
        [StringLength(100)]
        public string? CreatedBy { get; set; } = "";

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        public bool IsFilpride { get; set; }

        public bool IsMobility { get; set; }

        public bool IsBienes { get; set; }
    }
}
