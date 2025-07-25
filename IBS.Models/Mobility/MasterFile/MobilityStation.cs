using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility.Helpers;

namespace IBS.Models.Mobility.MasterFile
{
    public class MobilityStation
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StationId { get; set; }

        [Display(Name = "POS Code")]
        public string PosCode { get; set; }

        [Key]
        [Display(Name = "Station Code")]
        [Column(TypeName = "varchar(5)")]
        public string StationCode { get; set; }

        [Display(Name = "Station Name")]
        [Column(TypeName = "varchar(50)")]
        public string StationName { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string Initial { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "varchar(255)")]
        public string FolderPath { get; set; } = string.Empty;

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
