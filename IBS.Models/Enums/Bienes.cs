namespace IBS.Models.Enums
{
    public enum PlacementType
    {
        HoldOut,
        LongTerm,
        Others,
        ShortTerm
    }

    public enum PlacementStatus
    {
        Unposted,
        Posted,
        Locked,
        Terminated
    }

    public enum InterestStatus
    {
        NotApplicable,
        Withdrawn,
        Rolled,
    }
}
