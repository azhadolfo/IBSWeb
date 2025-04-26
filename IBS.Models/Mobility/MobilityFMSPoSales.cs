using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityFMSPoSales
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string StationCode { get; set; }

        public string ShiftRecordId { get; set; }

        public string ProductCode { get; set; }

        public string CustomerCode { get; set; }

        public string TripTicket { get; set; }

        public string DrNumber { get; set; }

        public string Driver { get; set; }

        public string PlateNo { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal ContractPrice { get; set; }

        public TimeOnly Time { get; set; }

        public DateOnly Date { get; set; }

        public DateOnly ShiftDate { get; set; }

        public int ShiftNumber { get; set; }

        public int PageNumber { get; set; }
    }
}
