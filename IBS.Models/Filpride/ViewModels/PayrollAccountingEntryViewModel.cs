namespace IBS.Models.Filpride.ViewModels
{
    public class PayrollAccountingEntryViewModel
    {
        public string AccountNumber { get; set; }
        public string AccountTitle { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public int? MultipleSupplierId { get; set; }
        public string? MultipleSupplierCodeName { get; set; }
    }
}
