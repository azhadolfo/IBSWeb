using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Enums;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideCollectionCategory
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public CollectionTaggingRequirement TaggingRequirement { get; set; }
        public bool AllowCompany { get; set; }
        public bool AllowEmployee { get; set; }
        public bool AllowBankAccount { get; set; }
        public bool IsActive { get; set; } = true;
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; }
        [StringLength(100)]
        public string? EditedBy { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? EditedDate { get; set; }

        public bool Allows(CollectionTagType type)
        {
            return type switch
            {
                CollectionTagType.Company => AllowCompany,
                CollectionTagType.Employee => AllowEmployee,
                CollectionTagType.BankAccount => AllowBankAccount,
                _ => false
            };
        }
    }
}
