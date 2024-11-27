using IBS.Utility;

namespace IBS.Models.Filpride.ViewModels
{
    public class AccountingEntryViewModel
    {
        public string AccountTitle { get; set; }
    
        public decimal Amount { get; set; }
    
        public bool Vatable { get; set; }
        
        public decimal TaxPercentage { get; set; }
    
        public decimal NetOfVatAmount { get; set; }

        public decimal VatAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public NormalBalance Type { get; set; }
    }
}