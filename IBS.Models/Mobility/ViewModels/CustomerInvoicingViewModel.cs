using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.ViewModels
{
    public class CustomerInvoicingViewModel
    {
        public int SalesHeaderId { get; set; }

        public List<SelectListItem>? DsrList { get; set; }

        public int MyProperty { get; set; }
    }
}