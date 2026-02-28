namespace IBS.Models.Enums
{
    public enum JvType
    {
        Liquidation,
        Accrual,
        Amortization,
        Reclass
    }

    public enum JvFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Annually
    }

    public enum JvStatus
    {
        Pending,
        ForApproval,
        Posted,
        Canceled,
        Voided,
    }
}
