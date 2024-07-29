using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility.MasterFile
{
    public class MobilitySupplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierId { get; set; }

        [Display(Name = "Supplier Name")]
        [Column(TypeName = "varchar(50)")]
        public string SupplierName { get; set; }

        [Display(Name = "Supplier Address")]
        [Column(TypeName = "varchar(200)")]
        public string SupplierAddress { get; set; }
    }
}