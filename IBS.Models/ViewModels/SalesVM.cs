using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.ViewModels
{
    public class SalesVM
    {
        public SalesHeader Header { get; set; }
        public IEnumerable<SalesDetail> Details { get; set; }
        public List<SelectListItem?> Offlines { get; set; }
    }
}