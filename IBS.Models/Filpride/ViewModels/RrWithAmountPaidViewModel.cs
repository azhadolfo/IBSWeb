using IBS.Models.Filpride.AccountsPayable;

namespace IBS.Models.Filpride.ViewModels
{
    public class RrWithAmountPaidViewModel
    {
        public FilprideReceivingReport ReceivingReport { get; set; } = null!;
        public decimal AmountPaid { get; set; }
    }
}
