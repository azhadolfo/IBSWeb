using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile;

public class Vessel : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int VesselId { get; set; }

    [StringLength(50)]
    public string? VesselCode { get; set; }

    [StringLength(200)]
    public string? VesselName { get; set; }
}
