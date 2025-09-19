using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI
{
    public class MMSICollection
    {
        [Key]
        public int MMSICollectionId { get; set; }

        public string MMSICollectionNumber
        {
            get => _collectionNumber;
            set => _collectionNumber = value.Trim();
        }

        private string _collectionNumber;

        public string CheckNumber { get; set; }

        public string Status { get; set; }

        public DateOnly Date { get; set; }

        public DateOnly CheckDate { get; set; }

        public DateOnly DepositDate { get; set; }

        public decimal Amount { get; set; }

        public decimal EWT { get; set; }

        public int CustomerId { get; set; }

        public bool IsUndocumented { get; set; }

        public string CreatedBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; }

        #region --Objects--

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        public List<MMSIBilling> PaidBills { get; set; }

        #endregion

    }
}
