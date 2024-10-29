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

        [Display(Name = "Customer Code")]
        [Column(TypeName = "varchar(7)")]
        [StringLength(7)]
        public string? CustomerCode { get; set; }

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
        public string CustomerAddress { get; set; }
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

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

        #region --Select List--

        [NotMapped]
        public List<SelectListItem>? MobilityStations { get; set; }

        #endregion --Select List--
    }
}