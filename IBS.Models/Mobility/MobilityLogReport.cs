using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Mobility
{
    public class MobilityLogReport
    {
        [Key]
        public Guid Id { get; set; }

        public string Reference { get; set; }

        public int ReferenceId { get; set; }

        public string Module { get; set; }

        public string Description { get; set; }

        public string? OriginalValue { get; set; }

        public string? AdjustedValue { get; set; }

        public DateTime TimeStamp { get; set; }

        public string ModifiedBy { get; set; }
    }
}
