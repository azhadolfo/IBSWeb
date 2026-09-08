using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap;

public class MsapBilling : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BillingId { get; set; }

    [StringLength(50)]
    public string? BillingNo { get; set; }

    [StringLength(200)]
    public string? CustomerName { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime BillingDate { get; set; } = DateTime.UtcNow;
}
