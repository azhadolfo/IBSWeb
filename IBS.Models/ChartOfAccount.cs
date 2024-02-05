using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models
{
    public class ChartOfAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public bool IsMain { get; set; }

        [Display(Name = "Account Number")]
        [Column(TypeName = "varchar(15)")]
        public string? Number { get; set; }

        [Display(Name = "Account Name")]
        [Column(TypeName = "varchar(100)")]
        public string Name { get; set; }

        [Column(TypeName = "varchar(15)")]
        public string? Type { get; set; }

        [Column(TypeName = "varchar(6)")]
        public string? Category { get; set; }

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
    }
}