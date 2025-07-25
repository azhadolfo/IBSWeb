using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI
{
    public class MMSIBilling
    {
        [Key]
        public int MMSIBillingId { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string MMSIBillingNumber
        {
            get => _billingNumber;
            set => _billingNumber = value.Trim();
        }

        private string _billingNumber;

        public DateOnly Date { get; set; }

        public string Status { get; set; }

        public bool IsUndocumented { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string BilledTo { get; set; }

        public string? VoyageNumber
        {
            get => _voyageNumber;
            set => _voyageNumber = value?.Trim();
        }

        private string? _voyageNumber;
        public decimal Amount { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? LastEditedBy { get; set; }

        public DateTime? LastEditedDate { get; set; }

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

        #region ---Address Lines---

        [NotMapped]
        public string? AddressLine1 { get; set; }

        [NotMapped]
        public string? AddressLine2 { get; set; }

        [NotMapped]
        public string? AddressLine3 { get; set; }

        [NotMapped]
        public string? AddressLine4 { get; set; }

        [NotMapped]
        public List<string>? UniqueTugboats { get; set; }

        #endregion

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
