using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MasterFile
{
    public class Company
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CompanyId { get; set; }

        [Display(Name = "Company Code")]
        [Column(TypeName = "varchar(3)")]
        public string? CompanyCode { get; set; }

        [Required]
        [Display(Name = "Company Name")]
        [Column(TypeName = "varchar(50)")]
        public string CompanyName { get; set; }

        [Required]
        [Display(Name = "Company Address")]
        [Column(TypeName = "varchar(200)")]
        public string CompanyAddress { get; set; }

        [Required]
        [Display(Name = "TIN No")]
        [Column(TypeName = "varchar(20)")]
        public string CompanyTin { get; set; }

        [Required]
        [Display(Name = "Business Style")]
        [Column(TypeName = "varchar(20)")]
        public string BusinessStyle { get; set; }

        public bool IsActive { get; set; } = true;

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
    }
}