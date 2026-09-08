using IBS.Models.Msap.MasterFile;

namespace IBS.Models.Msap.ViewModels
{
    public class TugboatOwnerViewModel : TugboatOwner
    {
        public TugboatOwnerViewModel() { }

        public TugboatOwnerViewModel(TugboatOwner entity)
        {
            TugboatOwnerId = entity.TugboatOwnerId;
            TugboatOwnerNumber = entity.TugboatOwnerNumber;
            TugboatOwnerName = entity.TugboatOwnerName;
            FixedRate = entity.FixedRate;
            MsapRecId = entity.MsapRecId;
        }
    }
}
