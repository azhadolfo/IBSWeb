using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile;

public class Employee : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EmployeeId { get; set; }

    [StringLength(50)]
    public string? EmployeeCode { get; set; }

    [StringLength(200)]
    public string? EmployeeName { get; set; }

    [StringLength(200)]
    public string? EmployeeAddress { get; set; }
}
