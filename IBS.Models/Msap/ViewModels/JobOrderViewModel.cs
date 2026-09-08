using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Msap.ViewModels
{
    public class JobOrderViewModel
    {
        public int JobOrderId { get; set; }

        [Required]
        [Display(Name = "Job Order Date")]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "Please select a customer")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a customer")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Please select a vessel")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a vessel")]
        [Display(Name = "Vessel")]
        public int VesselId { get; set; }

        [Required(ErrorMessage = "Please select a port")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a port")]
        [Display(Name = "Port")]
        public int PortId { get; set; }

        [Required(ErrorMessage = "Please select a terminal")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a terminal")]
        [Display(Name = "Terminal")]
        public int TerminalId { get; set; }

        [Display(Name = "COS Number")]
        [StringLength(20, ErrorMessage = "COS Number should not exceed 20 characters")]
        public string? COSNumber { get; set; }

        [Display(Name = "Voyage Number")]
        [StringLength(100, ErrorMessage = "Voyage Number should not exceed 100 characters")]
        public string? VoyageNumber { get; set; }

        [Display(Name = "Planned Start Time")]
        public DateTime? PlannedStartTime { get; set; }

        [Display(Name = "Planned End Time")]
        public DateTime? PlannedEndTime { get; set; }

        public string? Remarks { get; set; }

        #region ---Select Lists---

        public List<SelectListItem> Customers { get; set; } = new();

        public List<SelectListItem> Vessels { get; set; } = new();

        public List<SelectListItem> Ports { get; set; } = new();

        public List<SelectListItem> Terminals { get; set; } = new();

        #endregion
    }
}
