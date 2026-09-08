using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile
{
    public class MsapEmployee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeId { get; set; }

        [StringLength(10)]
        public string EmployeeNumber { get; set; } = null!;

        [StringLength(5)]
        public string? Initial { get; set; }

        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? MiddleName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(5)]
        public string? Suffix { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? BirthDate { get; set; }

        [StringLength(20)]
        public string? TelNo { get; set; }

        [StringLength(20)]
        public string? SssNo { get; set; }

        [RegularExpression(@"\d{3}-\d{3}-\d{3}-\d{5}", ErrorMessage = "Invalid TIN number format.")]
        [StringLength(20)]
        public string? TinNo { get; set; }

        [StringLength(20)]
        public string? PhilhealthNo { get; set; }

        [StringLength(20)]
        public string? PagibigNo { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Company { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Department { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateOnly DateHired { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? DateResigned { get; set; }

        [StringLength(50)]
        public string Position { get; set; } = null!;

        public bool IsManagerial { get; set; }

        [StringLength(20)]
        public string Supervisor { get; set; } = null!;

        [StringLength(50)]
        public string Status { get; set; } = null!;

        [StringLength(20)]
        public string? Paygrade { get; set; } = string.Empty;

        [Column(TypeName = "numeric(18,2)")]
        public decimal Salary { get; set; }

        public bool IsActive { get; set; }

        public string GetFullName()
        {
            return string.Join(" ",
                new[] { FirstName, MiddleName, LastName, Suffix }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }
}
