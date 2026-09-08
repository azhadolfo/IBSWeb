using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Msap
{
    public class TariffRate
    {
        [Key]
        public int TariffRateId { get; set; }

        public DateOnly AsOfDate { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public MsapCustomer Customer { get; set; } = null!;

        public int PortId { get; set; }

        [ForeignKey(nameof(PortId))]
        public Port Port { get; set; } = null!;

        public int TerminalId { get; set; }

        [ForeignKey(nameof(TerminalId))]
        public Terminal Terminal { get; set; } = null!;

        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public Service Service { get; set; } = null!;

        public decimal Dispatch { get; set; }

        public decimal BAF { get; set; }

        public string? CreatedBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? CreatedDate { get; set; }

        public string? UpdateBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? UpdateDate { get; set; }

        public decimal DispatchDiscount { get; set; }

        public decimal BAFDiscount { get; set; }

        #region -- Select Lists --

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }

        [NotMapped]
        public List<SelectListItem>? Services { get; set; }

        [NotMapped]
        public List<SelectListItem>? Terminals { get; set; }

        #endregion
    }
}

