using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideEmployee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeId { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string EmployeeNumber { get; set; }

        public string? Initial { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string FirstName { get; set; } = string.Empty;

        [Column(TypeName = "varchar(100)")]
        public string? MiddleName { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string LastName { get; set; } = string.Empty;

        [Column(TypeName = "varchar(5)")]
        public string? Suffix { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string? Address { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? BirthDate { get; set; }

        public string? TelNo { get; set; }

        public string? SssNo { get; set; }

        [RegularExpression(@"\d{3}-\d{3}-\d{3}-\d{5}", ErrorMessage = "Invalid TIN number format.")]
        [Column(TypeName = "varchar(20)")]
        public string? TinNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? PhilhealthNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? PagibigNo { get; set; } = string.Empty;

        [Column(TypeName = "varchar(20)")]
        public string? Company { get; set; } = string.Empty;

        [Column(TypeName = "varchar(20)")]
        public string? Depatment { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateOnly DateHired { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? DateResigned { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Position { get; set; }

        public bool IsManagerial { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Supervisor { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string Status { get; set; }

        public string? Paygrade { get; set; } = string.Empty;

        [Column(TypeName = "numeric(18,2)")]
        public decimal Salary { get; set; }

        public bool IsActive { get; set; }

    }
}
