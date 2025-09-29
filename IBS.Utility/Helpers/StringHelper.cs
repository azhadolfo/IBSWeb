namespace IBS.Utility.Helpers
{
    public static class StringHelper
    {
        public static string FormatRemarksWithSignatories(string? remarks, string? preparedByName = null)
        {
            var baseRemarks = string.IsNullOrWhiteSpace(remarks) ? "" : remarks.TrimEnd();

            // Use provided name or default to position only
            var preparedBy = string.IsNullOrWhiteSpace(preparedByName)
                ? "Trade & Supply"
                : $"{preparedByName}\nTrade & Supply";

            var notedBy = "Operations Manager";
            var approvedBy = "Chief Operations Officer";

            // Using string interpolation with padding for alignment
            var signatorySection = $@"

PREPARED BY:{new string(' ', 18)}NOTED BY:{new string(' ', 20)}APPROVED BY:
{preparedBy.PadRight(30)}{notedBy.PadRight(29)}{approvedBy}";

            return baseRemarks + signatorySection;
        }
    }
}
