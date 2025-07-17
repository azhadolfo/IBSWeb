using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilpridePickUpPoint
    {
        [Key]
        public int PickUpPointId { get; set; }

        [StringLength(50)]
        public string Depot { get; set; }

        [StringLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        [Column(TypeName = "timestamp with time zone")]
        public DateTime CreatedDate { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public FilprideSupplier? Supplier { get; set; }

        [StringLength(50)]
        public string Company { get; set; }

        [NotMapped]
        public List<SelectListItem>? Suppliers { get; set; }

        public bool IsFilpride { get; set; }

        public bool IsMobility { get; set; }

        public bool IsBienes { get; set; }
    }
}
