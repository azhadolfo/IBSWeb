using System.ComponentModel.DataAnnotations;
using IBS.Models.Enums;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public abstract class ProvisionalReceiptViewModel
    {
        [Required]
        [Display(Name = "Transaction Date")]
        public DateOnly TransactionDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a collection category.")]
        [Display(Name = "Collection Category")]
        public int CollectionCategoryId { get; set; }
        [EnumDataType(typeof(CollectionTagType))]
        [Display(Name = "Master-file Type")]
        public CollectionTagType? TagType { get; set; }
        [Display(Name = "Master-file Record")]
        public int? TagId { get; set; }
        [StringLength(200), Display(Name = "Received From")]
        public string? PayerName { get; set; }
        [StringLength(500), Display(Name = "Address")]
        public string? PayerAddress { get; set; }
        public List<FilprideCollectionCategory> Categories { get; set; } = [];
        public List<SelectListItem> TagOptions { get; set; } = [];

        [Required]
        [StringLength(50)]
        [Display(Name = "Reference No.")]
        public string ReferenceNo { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Remarks { get; set; }

        [Display(Name = "Cash Amount")]
        public decimal CashAmount { get; set; }

        [Display(Name = "Check Amount")]
        public decimal CheckAmount { get; set; }

        [Display(Name = "Check Date")]
        public DateOnly? CheckDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Check No.")]
        public string? CheckNo { get; set; }

        [StringLength(100)]
        [Display(Name = "Check Bank")]
        public string? CheckBank { get; set; }

        [StringLength(100)]
        [Display(Name = "Check Branch")]
        public string? CheckBranch { get; set; }

        [Display(Name = "Manager's Check Amount")]
        public decimal ManagersCheckAmount { get; set; }

        [Display(Name = "Manager's Check Date")]
        public DateOnly? ManagersCheckDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Manager's Check No.")]
        public string? ManagersCheckNo { get; set; }

        [StringLength(100)]
        [Display(Name = "Manager's Check Bank")]
        public string? ManagersCheckBank { get; set; }

        [StringLength(100)]
        [Display(Name = "Manager's Check Branch")]
        public string? ManagersCheckBranch { get; set; }

        public decimal EWT { get; set; }

        public decimal WVAT { get; set; }

        public decimal Total { get; set; }

        public DateTime MinDate { get; set; }

        [Display(Name = "Batch#")]
        public string? BatchNumber { get; set; }
    }
}
