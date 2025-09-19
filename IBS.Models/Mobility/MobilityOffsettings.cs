using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility.Helpers;

namespace IBS.Models.Mobility
{
    public class MobilityOffsettings
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OffSettingId { get; set; }

        public string AccountNo { get; set; }

        public string AccountTitle { get; set; }

        public string Source { get; set; }

        public string? Reference { get; set; }

        public bool IsRemoved { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        public string StationCode { get; set; } = string.Empty;
    }
}
