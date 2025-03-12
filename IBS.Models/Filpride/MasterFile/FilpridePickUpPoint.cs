using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilpridePickUpPoint
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
        public FilprideSupplier? Supplier { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Company { get; set; }

        [NotMapped]
        public List<SelectListItem>? Suppliers { get; set; }
    }
}
