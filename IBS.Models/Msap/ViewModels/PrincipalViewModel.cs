using IBS.Models.Msap.MasterFile;

namespace IBS.Models.Msap.ViewModels
{
    public class PrincipalViewModel : Principal
    {
        public PrincipalViewModel() { }

        public PrincipalViewModel(Principal entity)
        {
            PrincipalId = entity.PrincipalId;
            PrincipalNumber = entity.PrincipalNumber;
            PrincipalName = entity.PrincipalName;
            Agent = entity.Agent;
            Address1 = entity.Address1;
            Address2 = entity.Address2;
            Address3 = entity.Address3;
            BusinessType = entity.BusinessType;
            Terms = entity.Terms;
            TIN = entity.TIN;
            Landline1 = entity.Landline1;
            Landline2 = entity.Landline2;
            Mobile1 = entity.Mobile1;
            Mobile2 = entity.Mobile2;
            IsActive = entity.IsActive;
            IsVatable = entity.IsVatable;
            CustomerId = entity.CustomerId;
            MsapRecId = entity.MsapRecId;
        }
    }
}
