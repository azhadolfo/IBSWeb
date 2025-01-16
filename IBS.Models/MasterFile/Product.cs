using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility;
using IBS.Utility.Helpers;

namespace IBS.Models.MasterFile
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductId { get; set; }

        [Display(Name = "Product Code")]
        [Column(TypeName = "varchar(10)")]
        public string ProductCode { get; set; }

        [Display(Name = "Product Name")]
        [Column(TypeName = "varchar(50)")]
        public string ProductName { get; set; }

        [Display(Name = "Product Unit")]
        [Column(TypeName = "varchar(2)")]
        public string ProductUnit { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(50)")]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime EditedDate { get; set; }
    }
}