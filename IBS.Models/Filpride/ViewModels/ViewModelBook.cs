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

        //Additional filter/sorting can use for all reports
        [NotMapped]
        public List<SelectListItem>? SI { get; set; }

        [NotMapped]
        public List<SelectListItem>? SOA { get; set; }

        [NotMapped]
        public List<SelectListItem>? PO { get; set; }
    }
}