using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility;
using IBS.Utility.Helpers;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideService
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ServiceId { get; set; }

        [Display(Name = "Service No")]
        public string? ServiceNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? CurrentAndPreviousNo { get; set; }

        [Display(Name = "Current and Previous")]
        [Column(TypeName = "varchar(50)")]
        public string? CurrentAndPreviousTitle { get; set; }

        [NotMapped]
        public List<SelectListItem>? CurrentAndPreviousTitles { get; set; }

        [NotMapped]
        public int CurrentAndPreviousId { get; set; }

        [NotMapped]
        public int UnearnedId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? UnearnedTitle { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? UnearnedNo { get; set; }

        [NotMapped]
        public List<SelectListItem>? UnearnedTitles { get; set; }

        [Required]
        [Display(Name = "Service Name")]
        [Column(TypeName = "varchar(50)")]
        public string Name { get; set; }

        [Required]
        public int Percent { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        public string Company { get; set; } = string.Empty;

        public bool IsFilpride { get; set; }

        public bool IsMobility { get; set; }
    }
}
