using IBS.Models.Msap.MasterFile;

namespace IBS.Models.Msap.ViewModels
{
    public class TerminalViewModel : Terminal
    {
        public TerminalViewModel() { }

        public TerminalViewModel(Terminal entity)
        {
            TerminalId = entity.TerminalId;
            TerminalNumber = entity.TerminalNumber;
            TerminalName = entity.TerminalName;
            PortId = entity.PortId;
            MsapRecId = entity.MsapRecId;
        }
    }
}
