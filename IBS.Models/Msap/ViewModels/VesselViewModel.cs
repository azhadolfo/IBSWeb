using IBS.Models.Msap.MasterFile;

namespace IBS.Models.Msap.ViewModels
{
    public class VesselViewModel : Vessel
    {
        public VesselViewModel() { }

        public VesselViewModel(Vessel entity)
        {
            VesselId = entity.VesselId;
            VesselNumber = entity.VesselNumber;
            VesselName = entity.VesselName;
            VesselType = entity.VesselType;
            MsapRecId = entity.MsapRecId;
        }
    }
}
