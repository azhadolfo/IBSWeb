using IBS.Models.Msap.MasterFile;

namespace IBS.Models.Msap.ViewModels
{
    public class TugMasterViewModel : TugMaster
    {
        public TugMasterViewModel() { }

        public TugMasterViewModel(TugMaster entity)
        {
            TugMasterId = entity.TugMasterId;
            TugMasterNumber = entity.TugMasterNumber;
            TugMasterName = entity.TugMasterName;
            IsActive = entity.IsActive;
            MsapRecId = entity.MsapRecId;
        }
    }
}
