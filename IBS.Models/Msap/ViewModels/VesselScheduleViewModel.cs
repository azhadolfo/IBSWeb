using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Msap.ViewModels
{
    public class VesselScheduleViewModel
    {
        public int VesselScheduleId { get; set; }

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

        [Required(ErrorMessage = "Please enter planned start")]
        [Display(Name = "Planned Start")]
        public DateTime PlannedStart { get; set; }

        [Required(ErrorMessage = "Please enter planned end")]
        [Display(Name = "Planned End")]
        public DateTime PlannedEnd { get; set; }

        [Display(Name = "Required Tug Count")]
        [Range(1, 10, ErrorMessage = "Tug count must be between 1 and 10")]
        public int RequiredTugCount { get; set; } = 1;

        [Display(Name = "Assigned Tugboats")]
        public List<int>? SelectedTugboatIds { get; set; }

        [Display(Name = "Voyage Number")]
        [StringLength(50)]
        public string? VoyageNumber { get; set; }

        [Display(Name = "Vessel Type")]
        public string? VesselType { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Tentative";

        public string? Notes { get; set; }

        public int? JobOrderId { get; set; }

        #region ---Select Lists---

        public List<SelectListItem> Vessels { get; set; } = new();

        public List<SelectListItem> Ports { get; set; } = new();

        public List<SelectListItem> Terminals { get; set; } = new();

        public List<SelectListItem> Tugboats { get; set; } = new();

        public List<SelectListItem> Statuses { get; set; } = new();

        #endregion

        public VesselScheduleViewModel()
        {
            Statuses = new List<SelectListItem>
            {
                new() { Value = "Tentative", Text = "Tentative" },
                new() { Value = "Confirmed", Text = "Confirmed" },
                new() { Value = "In Progress", Text = "In Progress" },
                new() { Value = "Completed", Text = "Completed" },
                new() { Value = "Cancelled", Text = "Cancelled" }
            };
        }
    }
}
