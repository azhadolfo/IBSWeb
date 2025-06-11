using IBS.Models.MasterFile;
using IBS.Models.Mobility.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models.Mobility
{
    public class MobilityCustomerPurchaseOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerPurchaseOrderId { get; set; }
        [Display(Name = "PO No.")]
        public string CustomerPurchaseOrderNo { get; set; } = string.Empty;
        [Display(Name = "Date")]
        public DateOnly Date { get; set; }
        [Display(Name = "Quantity")]
        public decimal Quantity { get; set; }
        [Display(Name = "Price")]
        public decimal Price { get; set; }
        [Display(Name = "Total Amount")]
        public decimal Amount { get; set; }
        [Display(Name = "Customer Address")]
        public string Address { get; set; }

        #region Stations properties
        public int StationId { get; set; }
        public string StationCode { get; set; }

        [ForeignKey(nameof(StationCode))]
        public MobilityStation? MobilityStation { get; set; }
        #endregion

        #region Product's Properties
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
        #endregion

        #region Customer properties
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public MobilityCustomer? Customer { get; set; }
        #endregion

        #region-- Select List

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        [NotMapped]
        public List<SelectListItem>? MobilityStations { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        #endregion

    }
}
