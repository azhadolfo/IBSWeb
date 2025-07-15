using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IBS.Models.Filpride.ViewModels
{
    public class CustomerOrderSlipViewModel
    {
        public int CustomerOrderSlipId { get; set; }

        public DateOnly Date { get; set; }

        #region Customer properties

        [Required(ErrorMessage = "Customer field is required.")]
        public int CustomerId { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        [Display(Name = "Address")]
        public string? CustomerAddress { get; set; }

        [Display(Name = "TIN")]
        public string? TinNo { get; set; }

        public string? Terms { get; set; }

        public string? CustomerType { get; set; }

        #endregion Customer properties

        [StringLength(100)]
        [Required(ErrorMessage = "Customer PO No field is required.")]
        public string CustomerPoNo { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal Quantity { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal DeliveredPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Vat { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; }

        public bool HasCommission { get; set; }

        public int? CommissioneeId { get; set; }

        public List<SelectListItem>? Commissionee { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CommissionRate { get; set; }

        [StringLength(1000)]
        public string Remarks { get; set; }

        public string AccountSpecialist { get; set; }

        public int ProductId { get; set; }

        public List<SelectListItem>? Products { get; set; }

        public string? CurrentUser { get; set; }

        [StringLength(50)]
        public string OtcCosNo { get; set; }

        public string? Status { get; set; }

        public string? SelectedBranch { get; set; }

        public List<SelectListItem>? Branches { get; set; }

        public List<SelectListItem>? PurchaseOrder { get; set; }

        public string? StationCode { get; set; }

        public List<IFormFile>? UploadedFiles { get; set; }

        public List<COSFileInfo>? FileInfos { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Freight { get; set; }
    }

    public class COSFileInfo
    {
        public string FileName { get; set; }
        public string SignedUrl { get; set; }
    }
}
