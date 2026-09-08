using System.ComponentModel.DataAnnotations;
using IBS.Models.Enums;

namespace IBS.Models.Filpride.ViewModels
{
    public class CollectionCategoryViewModel
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [EnumDataType(typeof(CollectionTaggingRequirement))]
        [Display(Name = "Tagging requirement")]
        public CollectionTaggingRequirement TaggingRequirement { get; set; }
        [Display(Name = "Company")]
        public bool AllowCompany { get; set; }
        [Display(Name = "Employee")]
        public bool AllowEmployee { get; set; }
        [Display(Name = "Bank Account")]
        public bool AllowBankAccount { get; set; }
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        public bool IsUsed { get; set; }
    }
}
