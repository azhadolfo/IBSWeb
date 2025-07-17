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

        [StringLength(20)]
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

        [StringLength(100)]
        public string? UppiAtlNo
        {
            get => _uppiAtlNo;
            set => _uppiAtlNo = value?.Trim();
        }

        private string? _uppiAtlNo;

        [StringLength(255)]
        public string Remarks { get; set; }

        [StringLength(20)]
        public string CreatedBy { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime CreatedDate { get; set; }

        public ICollection<FilprideBookAtlDetail> Details { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public FilprideSupplier? Supplier { get; set; }

        [StringLength(20)]
        public string Company { get; set; }

        [StringLength(100)]
        public string? HaulerName { get; set; }

        [StringLength(200)]
        public string? Driver { get; set; }

        [StringLength(200)]
        public string? PlateNo { get; set; }

        [StringLength(100)]
        public string? SupplierName { get; set; }

        [StringLength(50)]
        public string Depot { get; set; }

        public int LoadPortId { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Freight { get; set; }

    }
}
