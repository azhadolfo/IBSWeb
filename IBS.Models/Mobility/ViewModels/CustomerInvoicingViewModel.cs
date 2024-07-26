using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.ViewModels
{
    public class CustomerPo
    {
        public int CustomerId { get; set; }
        public decimal PoAmount { get; set; }
    }

    public class ProductDetail
    {
        public int LubesId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class CustomerInvoicingViewModel
    {
        public int SalesHeaderId { get; set; }
        public List<SelectListItem>? DsrList { get; set; }

        #region--For PO
        public bool IncludePo { get; set; }
        public List<CustomerPo> CustomerPos { get; set; } = new List<CustomerPo>();
        public List<SelectListItem>? Customers { get; set; }
        #endregion

        #region--For Lubes
        public bool IncludeLubes { get; set; }
        public List<ProductDetail> ProductDetails { get; set; } = new List<ProductDetail>();
        public List<SelectListItem>? Lubes { get; set; }
        #endregion

        public string? User { get; set; }
    }
}