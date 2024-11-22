namespace IBS.Utility
{
    public enum CustomerType
    {
        Retail,
        Industrial,
        Government
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
        ApprovedByOM,
        Approved,
        Completed,
        Disapproved,
        Expired
    }

    public enum Status
    {
        Pending,
        Posted,
        Voided,
        Canceled
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
        BankAccount
    }

    public enum CVType
    {
        Invoicing,
        Payment
    }
}