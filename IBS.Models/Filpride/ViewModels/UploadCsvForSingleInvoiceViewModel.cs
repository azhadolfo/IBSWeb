namespace IBS.Models.Filpride.ViewModels
{
    public class UploadCsvForSingleInvoiceViewModel
    {
        public string CustomerName { get; set; }

        public string SalesInvoiceNo { get; set; }

        public DateOnly TransactionDate { get; set; }

        public string ReferenceNo { get; set; }

        public string Remarks { get; set; }

        public decimal CashAmount { get; set; }

        public DateOnly CheckDate { get; set; }

        public string CheckNo { get; set; }

        public string CheckBank { get; set; }

        public string CheckBranch { get; set; }

        public decimal CheckAmount { get; set; }

        public decimal ManagersCheckAmount { get; set; }

        public DateOnly ManagersCheckDate { get; set; }

        public string ManagersCheckNo { get; set; }

        public string ManagersCheckBank { get; set; }

        public string ManagersCheckBranch { get; set; }

        public decimal EWT { get; set; }

        public decimal WVAT { get; set; }

        public decimal Total { get; set; }

        public string BatchNumber { get; set; }

        public string Type { get; set; }
    }
}
