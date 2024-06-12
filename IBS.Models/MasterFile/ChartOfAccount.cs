using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MasterFile
{
    public class ChartOfAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AccountId { get; set; }

        public bool IsMain { get; set; }

        [Display(Name = "Account Number")]
        [Column(TypeName = "varchar(15)")]
        public string? AccountNumber { get; set; }

        [Display(Name = "Account Name")]
        [Column(TypeName = "varchar(100)")]
        public string AccountName { get; set; }

        [Column(TypeName = "varchar(25)")]
        public string? AccountType { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? NormalBalance { get; set; }

        public int Level { get; set; }

        [Column(TypeName = "varchar(15)")]
        public string? Parent { get; set; }

        [NotMapped]
        public List<SelectListItem>? Main { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(50)")]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime EditedDate { get; set; }


        // Select List

        [NotMapped]
        public List<SelectListItem>? Accounts { get; set; }
    }
}