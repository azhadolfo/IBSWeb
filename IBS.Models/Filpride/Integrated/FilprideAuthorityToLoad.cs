using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;

namespace IBS.Models.Filpride.Integrated
{
    public class FilprideAuthorityToLoad
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuthorityToLoadId { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string AuthorityToLoadNo { get; set; } = string.Empty;

        public int? CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        [Column(TypeName = "date")]
        public DateOnly DateBooked { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        [Column(TypeName = "date")]
        public DateOnly ValidUntil { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? UppiAtlNo { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string Remarks { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public ICollection<FilprideBookAtlDetail> Details { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public FilprideSupplier? Supplier { get; set; }

        public string Company { get; set; }
    }
}
