namespace IBS.DTOs
{
    public class ExportCsvDto
    {
        public class FilprideCollectionReceiptDto
        {
            public string DATE { get; set; }
            public string PAYEE { get; set; }
            public string CVNO { get; set; }
            public string CHECKNO { get; set; }
            public string PARTICULARS { get; set; }
            public decimal AMOUNT { get; set; }
            public string ACCOUNTNO { get; set; }
            public string CHECKDATE { get; set; }
            public bool ISORCANCEL { get; set; }
            public string DATEDEPOSITED { get; set; }
        }
    }
}
