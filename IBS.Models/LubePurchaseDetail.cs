using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class LubePurchaseDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LubeDeliveryDetailId { get; set; }

        public int LubeDeliveryHeaderId { get; set; }

        [ForeignKey("LubeDeliveryHeaderId")]
        public LubePurchaseHeader? LubeDeliveryHeader { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string Unit { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Description { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string ProductCode { get; set; }

        public int Piece { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

    }
}
