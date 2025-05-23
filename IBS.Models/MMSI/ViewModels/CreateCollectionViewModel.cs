using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.ViewModels
{
    public class CreateCollectionViewModel
    {
        public int? MMSICollectionId { get; set; }

        public string? MMSICollectionNumber { get; set; }

        public bool IsUndocumented { get; set; }

        public DateOnly Date { get; set; }

        public int CustomerId { get; set; }

        public decimal Amount { get; set; }

        public decimal EWT { get; set; }

        public string CheckNumber { get; set; }

        public DateOnly CheckDate { get; set; }

        public DateOnly DepositDate { get; set; }

        [NotMapped]
        public List<string>? ToCollectBillings { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Billings { get; set; }
    }
}
