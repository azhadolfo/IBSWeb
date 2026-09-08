using IBS.Models.Msap.MasterFile;

namespace IBS.Models.Msap.ViewModels
{
    public class TugboatViewModel : Tugboat
    {
        public TugboatViewModel() { }

        public TugboatViewModel(Tugboat entity)
        {
            TugboatId = entity.TugboatId;
            TugboatNumber = entity.TugboatNumber;
            TugboatName = entity.TugboatName;
            IsCompanyOwned = entity.IsCompanyOwned;
            TugboatOwnerId = entity.TugboatOwnerId;
            PortId = entity.PortId;
            MsapRecId = entity.MsapRecId;
        }
    }
}
