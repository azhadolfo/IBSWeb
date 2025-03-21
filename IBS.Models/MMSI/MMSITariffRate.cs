using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.MMSI.MasterFile;

namespace IBS.Models.MMSI
{
    public class MMSITariffRate
    {
        [Key]
        public int TariffRateId { get; set; }

        public DateOnly AsOfDate { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public MMSICustomer? Customer { get; set; }

        public int TerminalId { get; set; }

        [ForeignKey(nameof(TerminalId))]
        public MMSITerminal? Terminal { get; set; }

        public int ActivityServiceId {  get; set; }

        [ForeignKey(nameof(ActivityServiceId))]
        public MMSIActivityService? ActivityService { get; set; }

        public decimal Dispatch {  get; set; }

        public decimal BAF { get; set; }
    }
}
