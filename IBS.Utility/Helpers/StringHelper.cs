namespace IBS.Utility.Helpers
{
    public static class StringHelper
    {
        public static string FormatRemarksWithSignatories(string? remarks, string? preparedByName = null)
        {
            var baseRemarks = string.IsNullOrWhiteSpace(remarks) ? "" : remarks.TrimEnd();

            // Format prepared by section
            var preparedBy = string.IsNullOrWhiteSpace(preparedByName)
                ? ""
                : preparedByName;

            var preparedByPosition = "TRADE & SUPPLY";
            var notedBy = "OPERATIONS MANAGER";
            var approvedBy = "CHIEF OPERATIONS OFFICER";

            // Create the signatory section with proper spacing
            var signatorySection = string.IsNullOrWhiteSpace(preparedBy)
                ? $@"





PREPARED BY:{new string(' ', 18)}NOTED BY:{new string(' ', 20)}APPROVED BY:
{preparedByPosition.PadRight(30)}{notedBy.PadRight(29)}{approvedBy}"
                : $@"





PREPARED BY:{new string(' ', 18)}NOTED BY:{new string(' ', 20)}APPROVED BY:
{preparedBy.PadRight(30)}{string.Empty.PadRight(29)}
{preparedByPosition.PadRight(30)}{notedBy.PadRight(29)}{approvedBy}";

            return baseRemarks + signatorySection;
        }
    }
}
