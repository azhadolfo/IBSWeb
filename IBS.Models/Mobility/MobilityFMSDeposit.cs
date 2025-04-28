using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityFMSDeposit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string StationCode { get; set; }

        public string ShiftRecordId { get; set; }

        public DateOnly Date { get; set; }

        public string AccountNumber { get; set; }

        public decimal Amount { get; set; }

        public DateOnly ShiftDate { get; set; }

        public int ShiftNumber { get; set; }

        public int PageNumber { get; set; }
    }
}
