using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile
{
    public class MsapPostedPeriod
    {
        [Key]
        public int Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public bool IsClosed { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? ClosedBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? ClosedDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? OpenedBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? OpenedDate { get; set; }
    }
}
