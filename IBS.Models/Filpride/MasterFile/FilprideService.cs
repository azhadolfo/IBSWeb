using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideService
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ServiceId { get; set; }

        [StringLength(5)]
        [Display(Name = "Service No")]
        public string? ServiceNo { get; set; }

        [StringLength(20)]
        public string? CurrentAndPreviousNo { get; set; }

        [Display(Name = "Current and Previous")]
        [StringLength(50)]
        public string? CurrentAndPreviousTitle { get; set; }

        [NotMapped]
        public List<SelectListItem>? CurrentAndPreviousTitles { get; set; }

        [NotMapped]
        public int CurrentAndPreviousId { get; set; }

        [NotMapped]
        public int UnearnedId { get; set; }

        [StringLength(50)]
        public string? UnearnedTitle { get; set; }

        [StringLength(20)]
        public string? UnearnedNo { get; set; }

        [NotMapped]
        public List<SelectListItem>? UnearnedTitles { get; set; }

        [Required]
        [Display(Name = "Service Name")]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        public int Percent { get; set; }

        [Display(Name = "Created By")]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

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
