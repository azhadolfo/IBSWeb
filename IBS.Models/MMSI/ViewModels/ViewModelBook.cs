using System.ComponentModel.DataAnnotations;

namespace IBS.Models.MMSI.ViewModels
{
    public class ViewModelBook
    {
        [Display(Name = "Date From")]
        public DateOnly DateFrom { get; set; }

        [Display(Name = "Date To")]
        public DateOnly DateTo { get; set; }
    }
}
