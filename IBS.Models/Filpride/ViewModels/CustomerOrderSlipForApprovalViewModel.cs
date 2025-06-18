using IBS.Models.Filpride.Integrated;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class CustomerOrderSlipForApprovalViewModel
    {
        #region Ops Manager Approval

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal NetOfVatProductCost { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal NetOfVatCosPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal NetOfVatFreightCharge { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal GrossMargin { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal VatAmount { get; set; }

        #endregion

        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        #region Finance Approval

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CreditBalance { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Total { get; set; }

        #endregion

        public string Status { get; set; }

        public string PriceReference { get; set; }

        public List<COSFileInfo> UploadedFiles { get; set; }
    }
}
