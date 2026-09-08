using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Msap
{
    public class Billing : BaseEntity
    {
        [Key]
        [Column("RECID")]
        public int MsapBillingId { get; set; }

        [Column("NUMBER", TypeName = "varchar(10)")]
        public string MsapBillingNumber
        {
            get => _billingNumber;
            set => _billingNumber = value.Trim();
        }

        private string _billingNumber = null!;

        [Column("DATE")]
        public DateOnly Date { get; set; }

        public string Status { get; set; } = null!;

        public bool IsUndocumented { get; set; }

        [Column("CUSTNO", TypeName = "varchar(10)")]
        public string BilledTo { get; set; } = null!;

        public string? VoyageNumber
        {
            get => _voyageNumber;
            set => _voyageNumber = value?.Trim();
        }

        private string? _voyageNumber;

        [Column(TypeName = "varchar(20)")]
        public string? COSNumber
        {
            get => _cosNumber;
            set => _cosNumber = value?.Trim();
        }

        private string? _cosNumber;

        [Column("AMOUNT", TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Balance { get; set; }

        public bool IsPaid { get; set; }

        public decimal DispatchAmount { get; set; }

        public decimal BAFAmount { get; set; }

        public bool IsPrincipal { get; set; }

        [Column("CUSTNO_FK")] // Keeping CustomerId but noting it links to CUSTNO logic
        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public MsapCustomer Customer { get; set; } = null!;

        public int? PrincipalId { get; set; }
        [ForeignKey(nameof(PrincipalId))]
        public Principal? Principal { get; set; }

        [Column("VESSELNUM")]
        public int VesselId { get; set; }
        [ForeignKey(nameof(VesselId))]
        public Vessel Vessel { get; set; } = null!;

        [Column("PORTNUM")]
        public int PortId { get; set; }
        [ForeignKey(nameof(PortId))]
        public Port Port { get; set; } = null!;

        [Column("TERMINAL")]
        public int TerminalId { get; set; }
        [ForeignKey(nameof(TerminalId))]
        public Terminal Terminal { get; set; } = null!;

        public decimal ApOtherTug { get; set; }

        [Column("VAT")]
        public bool IsVatable { get; set; }

        public bool IsVatInclusive { get; set; } = true;

        public bool PrintWht { get; set; }

        public bool IsPrinted { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }

        [StringLength(15)]
        public string? Terms { get; set; }

        [Column("billing_year")]
        public int Year { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

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
        public Dictionary<int, decimal> BafRates { get; set; } = new();

        [NotMapped]
        public Dictionary<int, string> BafChargeTypes { get; set; } = new();

        [NotMapped]
        public List<DispatchTicket>? PaidDispatchTickets { get; set; }

        public int? JobOrderId { get; set; }

        [ForeignKey(nameof(JobOrderId))]
        public JobOrder? JobOrder { get; set; }

        [Column("CRNUM")]
        public int? CollectionId { get; set; }

        [ForeignKey(nameof(CollectionId))]
        public Collection? Collection { get; set; }

        public string? CollectionNumber { get; set; }

        [Column("unposted_by")]
        public string? UnpostedBy { get; set; }

        [Column("unposted_date")]
        public DateTime? UnpostedDate { get; set; }

        [Column("unpost_remarks")]
        public string? UnpostRemarks { get; set; }

        [NotMapped]
        public List<SelectListItem>? CustomerPrincipal { get; set; }

        #endregion ---Select Lists---

        [NotMapped]
        public bool IsMonthClosed { get; set; }

        [NotMapped]
        public string? DispatchTicketNumbers { get; set; }
    }
}

