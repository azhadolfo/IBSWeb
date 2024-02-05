using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class Company
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Company Code")]
        public int Code { get; set; }

        [Required]
        [Display(Name = "Company Name")]
        [Column(TypeName = "varchar(50)")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Customer Address")]
        [Column(TypeName = "varchar(200)")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "TIN No")]
        [Column(TypeName = "varchar(20)")]
        public string TinNo { get; set; }

        [Required]
        [Display(Name = "Business Style")]
        [Column(TypeName = "varchar(20)")]
        public string BusinessStyle { get; set; }

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