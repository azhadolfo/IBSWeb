using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.MasterFile
{
    public class MobilityPickUpPoint
    {
        [Key]
        public int PickUpPointId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Depot { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string CreatedBy { get; set; } = string.Empty;

        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public MobilitySupplier? Supplier { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string StationCode { get; set; }

        [NotMapped]
        public List<SelectListItem>? Suppliers { get; set; }
    }
}
