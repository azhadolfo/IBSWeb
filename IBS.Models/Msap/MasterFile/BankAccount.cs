using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile;

public class BankAccount : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BankAccountId { get; set; }

    [StringLength(50)]
    public string? Bank { get; set; }

    [StringLength(200)]
    public string? Branch { get; set; }

    [StringLength(50)]
    public string? AccountNo { get; set; }

    [StringLength(200)]
    public string? AccountName { get; set; }
}
