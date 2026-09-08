using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Msap.MasterFile
{
    public class UserAccess
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        [Column(TypeName = "varchar(100)")]
        public string? UserName { get; set; }

        #region -- MSAP Workflow --

        public bool CanCreateServiceRequest { get; set; }

        public bool CanPostServiceRequest { get; set; }

        public bool CanCreateDispatchTicket { get; set; }

        public bool CanEditDispatchTicket { get; set; }

        [Column("can_cancel_dispatch_ticket")]
        public bool CanDeleteDispatchTicket { get; set; }

        public bool CanSetTariff { get; set; }

        public bool CanApproveTariff { get; set; }

        public bool CanCreateBilling { get; set; }

        public bool CanEditBilling { get; set; }

        public bool CanDeleteBilling { get; set; }

        public bool CanReverseBilling { get; set; }

        public bool CanCreateCollection { get; set; }

        public bool CanCreateJobOrder { get; set; }

        public bool CanEditJobOrder { get; set; }

        public bool CanDeleteJobOrder { get; set; }

        public bool CanCloseJobOrder { get; set; }

        #endregion -- MSAP Workflow --

        #region -- Treasury --

        public bool CanAccessTreasury { get; set; }

        public bool CanCreateDisbursement { get; set; }

        #endregion -- Treasury --

        #region -- MSAP Import --

        public bool CanManageMsapImport { get; set; }

        #endregion -- MSAP Import --

        #region -- Master Files --

        public bool CanManageMaritimeMasterFile { get; set; }

        #endregion -- Master Files --

        #region -- Reports --

        public bool CanViewGeneralLedger { get; set; }

        public bool CanViewInventoryReport { get; set; }

        public bool CanViewMaritimeReport { get; set; }

        #endregion -- Reports --

        [NotMapped]
        public List<SelectListItem>? Users { get; set; }
    }
}
