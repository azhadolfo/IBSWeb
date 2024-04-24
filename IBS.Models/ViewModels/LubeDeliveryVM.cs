namespace IBS.Models.ViewModels
{
    public class LubeDeliveryVM
    {
        public LubePurchaseHeader Header { get; set; }

        public IEnumerable<LubePurchaseDetail> Details { get; set; }

    }
}
