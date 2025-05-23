using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.ViewModels
{
    public class CreateBillingViewModel
    {
        public int? MMSIBillingId { get; set; }

        public string? MMSIBillingNumber { get; set; }

        public DateOnly Date {  get; set; }

        public bool IsUndocumented { get; set; }

        public string BilledTo { get; set; }

        public string? VoyageNumber { get; set; }

        public decimal Amount { get; set; }

        public bool IsPrincipal { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        public int? PrincipalId { get; set; }
        [ForeignKey(nameof(PrincipalId))]
        public MMSIPrincipal? Principal { get; set; }

        public int VesselId { get; set; }
        [ForeignKey(nameof(VesselId))]
        public MMSIVessel? Vessel { get; set; }

        public int PortId { get; set; }
        [ForeignKey(nameof(PortId))]
        public MMSIPort? Port { get; set; }

        public int TerminalId { get; set; }
        [ForeignKey(nameof(TerminalId))]
        public MMSITerminal? Terminal { get; set; }

        #region ---Select Lists---

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Vessels { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }

        [NotMapped]
        public List<SelectListItem>? Terminals { get; set; }

        [NotMapped]
        public List<SelectListItem>? UnbilledDispatchTickets { get; set; }

        [NotMapped]
        public List<string>? ToBillDispatchTickets { get; set; }

        [NotMapped]
        public List<MMSIDispatchTicket>? PaidDispatchTickets { get; set; }

        public int? CollectionId { get; set; }

        [NotMapped]
        public MMSICollection? Collection { get; set; }

        [NotMapped]
        public List<SelectListItem>? CustomerPrincipal { get; set; }

        #endregion ---Select Lists---
    }
}
