namespace IBS.Utility.Helpers
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo PhilippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

        public static DateTime GetCurrentPhilippineTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhilippineTimeZone);
        }

        public static string GetCurrentPhilippineTimeFormatted(DateTime dateTime = default, string format = "MM/dd/yyyy hh:mm tt")
        {
            var philippineTime = dateTime != default ? dateTime : GetCurrentPhilippineTime();
            return philippineTime.ToString(format);
        }
    }
}
