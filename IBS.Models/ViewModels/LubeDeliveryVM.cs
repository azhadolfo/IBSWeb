using IBS.Models.Mobility;

namespace IBS.Models.ViewModels
{
    public class LubeDeliveryVM
    {
        public MobilityLubePurchaseHeader Header { get; set; }

        public IEnumerable<MobilityLubePurchaseDetail> Details { get; set; }

    }
}
