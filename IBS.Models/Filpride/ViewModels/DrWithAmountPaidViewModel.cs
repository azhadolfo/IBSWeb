using IBS.Models.Filpride.Integrated;

namespace IBS.Models.Filpride.ViewModels
{
    public class DrWithAmountPaidViewModel
    {
        public FilprideDeliveryReceipt DeliveryReceipt { get; set; } = null!;
        public decimal AmountPaid { get; set; }
    }

    public record MonthYear(int Year, int Month);
}
