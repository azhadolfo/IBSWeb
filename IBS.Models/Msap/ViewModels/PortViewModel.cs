using IBS.Models.Msap.MasterFile;

namespace IBS.Models.Msap.ViewModels
{
    public class PortViewModel : Port
    {
        public PortViewModel() { }

        public PortViewModel(Port entity)
        {
            PortId = entity.PortId;
            PortNumber = entity.PortNumber;
            PortName = entity.PortName;
            HasSBMA = entity.HasSBMA;
            MsapRecId = entity.MsapRecId;
        }
    }
}
