using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityLubePurchaseDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LubePurchaseDetailId { get; set; }

        public int LubePurchaseHeaderId { get; set; }

        [ForeignKey(nameof(LubePurchaseHeaderId))]
        public MobilityLubePurchaseHeader? LubePurchaseHeader { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string LubePurchaseHeaderNo { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string Unit { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Description { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal CostPerCase { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal CostPerPiece { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string ProductCode { get; set; }

        public int Piece { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; }
    }
}