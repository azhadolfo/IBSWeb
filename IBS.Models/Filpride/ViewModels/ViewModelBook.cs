using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.ViewModels
{
    public class ViewModelBook
    {
        [Display(Name = "Date From")]
        public DateOnly DateFrom { get; set; }

        [Display(Name = "Date To")]
        public DateOnly DateTo { get; set; }

        // New property for "As of" date selection
        [Display(Name = "As of (Month and Year)")]
        public DateOnly? AsOfMonthYear { get; set; }

        // Helper method to calculate "as of" date (last day of selected month)
        public void SetAsOfDate()
        {
            if (AsOfMonthYear.HasValue)
            {
                // Set DateTo to the last day of the selected month
                var year = AsOfMonthYear.Value.Year;
                var month = AsOfMonthYear.Value.Month;
                DateTo = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            }
        }

        #region Filtering

        public List<SelectListItem>? SI { get; set; }

        public List<SelectListItem>? SOA { get; set; }

        public List<SelectListItem>? PO { get; set; }

        public List<int>? Customers { get; set; }
        public List<int>? Commissionee { get; set; }

        #region Gross Margin

        public List<SelectListItem>? CustomerList { get; set; }

        public List<SelectListItem>? CommissioneeList { get; set; }

        #endregion

        #region Purchase Report

        // New property for date selection type
        [Display(Name = "Date Selection Based On")]
        public string DateSelectionType { get; set; } = "RRDate";

        #endregion

        #region Liquidation Report

        public List<SelectListItem>? SupplierList { get; set; }

        public int? PurchaseOrderId { get; set; }

        public DateOnly? Period { get; set; }

        #endregion

        #endregion
    }
}
