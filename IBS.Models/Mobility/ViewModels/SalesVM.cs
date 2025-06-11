using IBS.Models.Mobility;

namespace IBS.Models.Mobility.ViewModels
{
    public class SalesVM
    {
        public MobilitySalesHeader? Header { get; set; }
        public IEnumerable<MobilitySalesDetail> Details { get; set; }
    }
}
