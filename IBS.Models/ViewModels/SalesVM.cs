using IBS.Models.Mobility;

namespace IBS.Models.ViewModels
{
    public class SalesVM
    {
        public SalesHeader Header { get; set; }
        public IEnumerable<SalesDetail> Details { get; set; }
    }
}