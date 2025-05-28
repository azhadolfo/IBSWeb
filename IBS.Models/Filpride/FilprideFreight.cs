using IBS.Models.Filpride.MasterFile;
using IBS.Utility;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility.Enums;

namespace IBS.Models.Filpride
{
    public class FilprideFreight
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int FreightId { get; set; }

        public int PickUpPointId { get; set; }

        [ForeignKey(nameof(PickUpPointId))]
        public FilpridePickUpPoint? PickUpPoint { get; set; }

        public ClusterArea ClusterCode { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Freight { get; set; }
    }
}
