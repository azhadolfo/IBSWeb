using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class PostedPeriod
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(50)]
        public string Company { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public bool IsPosted { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime PostedOn { get; set; }

        [StringLength(50)]
        public string PostedBy { get; set; }
    }
}
