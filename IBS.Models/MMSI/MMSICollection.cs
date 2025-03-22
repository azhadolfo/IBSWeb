using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI
{
    public class MMSICollection
    {
        [Key]
        public int MMSICollectionId { get; set; }

        public string CollectionNumber { get; set; }

        public string CheckNumber { get; set; }

        public string Status { get; set; }

        public DateOnly Date { get; set; }

        public DateOnly CheckDate { get; set; }

        public DateOnly DepositDate { get; set; }

        public decimal Amount { get; set; }

        public decimal EWT { get; set; }

        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public MMSICustomer Customer { get; set; }

        [NotMapped]
        public List<MMSIBilling> PaidBills { get; set; }

        [NotMapped]
        public List<SelectListItem> Billings { get; set; }

        [NotMapped]
        public List<string>? ToCollectBillings { get; set; }

    }
}
