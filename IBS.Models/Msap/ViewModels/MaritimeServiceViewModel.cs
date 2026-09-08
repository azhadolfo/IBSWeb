using IBS.Models.Msap.MasterFile;

namespace IBS.Models.Msap.ViewModels
{
    public class MaritimeServiceViewModel : Service
    {
        public MaritimeServiceViewModel() { }

        public MaritimeServiceViewModel(Service entity)
        {
            ServiceId = entity.ServiceId;
            ServiceNumber = entity.ServiceNumber;
            ServiceName = entity.ServiceName;
            MsapRecId = entity.MsapRecId;
        }
    }
}
