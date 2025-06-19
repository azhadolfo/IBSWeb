namespace IBS.DTOs
{
    public class ExportCsvDto
    {
        public class FilprideCollectionReceiptCsvForDcrDto
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

        public class FilprideCheckVoucherHeaderCsvForDcrDto
        {
            public string VOUCHER_NO { get; set; }
            public string VCH_DATE { get; set; }
            public string PAYEE { get; set; }
            public decimal AMOUNT { get; set; }
            public string PARTICULARS { get; set; }
            public string CHECKNO { get; set; }
            public string CHKDATE { get; set; }
            public string ACCOUNTNO { get; set; }
            public string CASHPODATE { get; set; }
            public string DCRDATE { get; set; }
            public bool ISCANCELLED { get; set; }
        }
    }
}
