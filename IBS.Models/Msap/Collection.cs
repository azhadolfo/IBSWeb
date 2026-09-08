using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap;

public class MsapCollection : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CollectionId { get; set; }

    [StringLength(50)]
    public string? CollectionNo { get; set; }

    [StringLength(200)]
    public string? CustomerName { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime CollectionDate { get; set; } = DateTime.UtcNow;
}
