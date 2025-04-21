using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityFMSCalibration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string StationCode { get; set; }

        public string ShiftRecordId { get; set; }

        public int PumpNumber { get; set; }

        public string ProductCode { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }

        public DateOnly ShiftDate { get; set; }

        public int ShiftNumber { get; set; }

        public int PageNumber { get; set; }
    }
}
