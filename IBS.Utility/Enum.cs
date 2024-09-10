﻿namespace IBS.Utility
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

    public enum SalesInvoiceType
    {
        Documented,
        Undocumented
    }

    public enum CosStatus
    {
        Created,
        SupplierAppointed,
        ApprovedByOpsManager,
        ApprovedByFinance,
        HaulerAppointed,
        Completed,
        Disapproved
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
}