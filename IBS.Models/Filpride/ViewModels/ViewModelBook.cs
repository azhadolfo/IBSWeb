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

        #region Filtering

        public List<SelectListItem>? SI { get; set; }

        public List<SelectListItem>? SOA { get; set; }

        public List<SelectListItem>? PO { get; set; }

        public List<int>? Customers { get; set; }

        #region Gross Margin

        public List<SelectListItem>? CustomerList { get; set; }

        #endregion

        #region Purchase Report

        // New property for date selection type
        [Display(Name = "Date Selection Based On")]
        public string DateSelectionType { get; set; } = "RRDate";

        #endregion



        #endregion
    }
}
