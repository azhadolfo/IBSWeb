using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MasterFile
{
    public class Hauler
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int HaulerId { get; set; }

        [Column(TypeName = "varchar(3)")]
        [Display(Name = "Hauler Code")]
        public string? HaulerCode { get; set; }

        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Hauler Name")]
        public string HaulerName { get; set; }

        [Column(TypeName = "varchar(200)")]
        [Display(Name = "Hauler Address")]
        public string HaulerAddress { get; set; }

        [Column(TypeName = "varchar(15)")]
        [Display(Name = "Contact No")]
        public string ContactNo { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(100)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(100)")]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime? EditedDate { get; set; }
    }
}