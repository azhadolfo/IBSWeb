using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap;

public class MsapJobOrder : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int JobOrderId { get; set; }

    [StringLength(50)]
    public string? JobOrderNo { get; set; }

    [StringLength(200)]
    public string? VesselName { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime JobOrderDate { get; set; } = DateTime.UtcNow;
}
