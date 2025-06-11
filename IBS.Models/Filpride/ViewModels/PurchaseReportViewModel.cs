using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class PurchaseReportViewModel
    {
        [Key]
        public int PurchaseReportId { get; set; }

        [Display(Name = "Date")]
        public DateOnly Date { get; set; }

        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; }

        [Display(Name = "Supplier TIN")]
        public string SupplierTin { get; set; }

        [Display(Name = "Supplier Address")]
        public string SupplierAddress { get; set; }

        [Display(Name = "PO No.")]
        public string PurchaseOrderNo { get; set; }

        [Display(Name = "Filpride RR")]
        public string FilprideRR { get; set; }

        [Display(Name = "Filpride DR")]
        public string FilprideDR { get; set; }

        [Display(Name = "ATL No")]
        public string ATLNo { get; set; }

        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [Display(Name = "Product")]
        public string Product { get; set; }

        [Display(Name = "Volume")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal Volume { get; set; }

        [Display(Name = "Cost Per Liter")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? CostPerLiter { get; set; }

        [Display(Name = "Cost Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? CostAmount { get; set; }

        [Display(Name = "Vat Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? VATAmount { get; set; }

        [Display(Name = "Def Vat Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? DefVatAmount { get; set; }

        [Display(Name = "WHT Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? WHTAmount { get; set; }

        [Display(Name = "Net Purchase")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? NetPurchases { get; set; }

        [Display(Name = "Account Specialist")]
        public string AccountSpecialist { get; set; }

        [Display(Name = "COS Price")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? COSPrice { get; set; }

        [Display(Name = "COS Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? COSAmount { get; set; }

        [Display(Name = "COS Per Liter")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? COSPerLiter { get; set; }

        [Display(Name = "GM Per Liter")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? GMPerLiter { get; set; }

        [Display(Name = "GM Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? GMAmount { get; set; }

        [Display(Name = "Hauler Name")]
        public string? HaulerName { get; set; }

        [Display(Name = "Freight Charge")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? FreightCharge { get; set; }

        [Display(Name = "FC Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? FCAmount { get; set; }

        [Display(Name = "Commission Per Liter")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? CommissionPerLiter { get; set; }

        [Display(Name = "Commission Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? CommissionAmount { get; set; }

        [Display(Name = "Net Margin Per Liter")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? NetMarginPerLiter { get; set; }

        [Display(Name = "Net Margin Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? NetMarginAmount { get; set; }

        [Display(Name = "Supplier Sales Invoice")]
        public string SupplierSalesInvoice { get; set; }

        [Display(Name = "Supplier DR")]
        public string SupplierDR { get; set; }

        [Display(Name = "Supplier WC")]
        public string SupplierWC { get; set; }
    }

}
