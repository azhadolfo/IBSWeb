using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI
{
    public class MMSITariffRate
    {
        [Key]
        public int TariffRateId { get; set; }

        public DateOnly AsOfDate { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        public int TerminalId { get; set; }

        [ForeignKey(nameof(TerminalId))]
        public MMSITerminal? Terminal { get; set; }

        public int ActivityServiceId {  get; set; }

        [ForeignKey(nameof(ActivityServiceId))]
        public MMSIActivityService? ActivityService { get; set; }

        public decimal Dispatch {  get; set; }

        public decimal BAF { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string? UpdateBy { get; set; }

        public DateTime? UpdateDate { get; set; }

        public decimal DispatchDiscount { get; set; }

        public decimal BAFDiscount { get; set; }

        #region -- Select Lists --

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }

        [NotMapped]
        public List<SelectListItem>? ActivitiesServices { get; set; }

        [NotMapped]
        public List<SelectListItem>? Terminals { get; set; }

        #endregion
    }
}
