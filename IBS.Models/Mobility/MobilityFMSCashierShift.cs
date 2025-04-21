using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityFMSCashierShift
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string StationCode { get; set; }

        public string ShiftRecordId { get; set; }

        public DateOnly Date { get; set; }

        public string EmployeeNumber { get; set; }

        public int ShiftNumber { get; set; }

        public int PageNumber { get; set; }

        public TimeOnly TimeIn { get; set; }

        public TimeOnly TimeOut { get; set; }

        public bool NextDay { get; set; }

        public decimal CashOnHand { get; set; }

        public decimal BiodieselPrice { get; set; }

        public decimal EconogasPrice { get; set; }

        public decimal EnvirogasPrice { get; set; }
    }
}
