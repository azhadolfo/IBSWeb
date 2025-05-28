namespace IBS.Utility.Enums
{
    public enum CustomerType
    {
        Retail,
        Industrial,
        Government,
        Reseller
    }

    public enum JournalType
    {
        Sales,
        Purchase,
        Inventory
    }

    public enum NormalBalance
    {
        Debit,
        Credit
    }

    public enum DocumentType
    {
        Documented,
        Undocumented
    }

    public enum CosStatus
    {
        Created,
        SupplierAppointed,
        HaulerAppointed,
        ForAtlBooking,
        ForApprovalOfOM,
        ForApprovalOfFM,
        ForDR,
        Completed,
        Disapproved,
        Expired,
        Closed
    }

    public enum Status
    {
        Pending,
        Posted,
        Voided,
        Canceled
    }

    public enum DRStatus
    {
        PendingDelivery,
        ForInvoicing,
        Invoiced,
        ForApprovalOfOM,
        Canceled,
        Voided
    }

    public enum ClusterArea
    {
        South,
        North,
        Marinduque,
        Bacolod
    }

    public enum DynamicView
    {
        SalesInvoice,
        ServiceInvoice,
        CollectionReceipt,
        DebitMemo,
        CreditMemo,
        PurchaseOrder,
        ReceivingReport,
        CheckVoucher,
        JournalVoucher,
        Customer,
        Product,
        Supplier,
        Service,
        BankAccount,
        ChartOfAccount
    }

    public enum CVType
    {
        Invoicing,
        Payment
    }

}
