using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSIUserAccess
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? UserName { get; set; }

        public bool CanCreateServiceRequest { get; set; }

        public bool CanPostServiceRequest { get; set; }

        public bool CanCreateDispatchTicket { get; set; }

        public bool CanSetTariff { get; set; }

        public bool CanApproveTariff { get; set; }

        public bool CanCreateBilling { get; set; }

        public bool CanCreateCollection { get; set; }

        public bool CanPrintReport { get; set; }

        [NotMapped]
        public List<SelectListItem>? Users { get; set; }
    }
}
