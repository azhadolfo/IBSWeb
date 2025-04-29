using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityFMSLubeSales
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string StationCode { get; set; }

        public string ShiftRecordId { get; set; }

        public string ProductCode { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal ActualPrice { get; set; }

        public decimal Cost { get; set; }

        public DateOnly ShiftDate { get; set; }

        public int ShiftNumber { get; set; }

        public int PageNumber { get; set; }

        public bool IsProcessed { get; set; }
    }
}
