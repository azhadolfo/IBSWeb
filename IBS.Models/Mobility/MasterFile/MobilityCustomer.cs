using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility.MasterFile
{
    public class MobilityCustomer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        [Display(Name = "Customer Name")]
        [Column(TypeName = "varchar(50)")]
        [StringLength(50)]
        public string CustomerName { get; set; }

        [Display(Name = "Code Name")]
        [Column(TypeName = "varchar(10)")]
        [StringLength(10)]
        public string CustomerCodeName { get; set; }

        [Display(Name = "Station Code")]
        [Column(TypeName = "varchar(3)")]
        [StringLength(3)]
        public string StationCode { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string CustomerAddress { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal AmountLimit { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal QuantityLimit { get; set; }

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(50)")]
        [StringLength(50)]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime? EditedDate { get; set; }

        [Required]
        [Display(Name = "Payment Terms")]
        [Column(TypeName = "varchar(10)")]
        public string CustomerTerms { get; set; }

        [Display(Name = "Customer TIN")]
        public string CustomerTin { get; set; }

        [Display(Name = "Customer Type")]
        [Column(TypeName = "varchar(10)")]
        public string CustomerType { get; set; }

        #region --Select List--

        [NotMapped]
        public List<SelectListItem>? MobilityStations { get; set; }

        #endregion --Select List--
    }
}