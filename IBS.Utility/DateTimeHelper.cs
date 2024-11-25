namespace IBS.Utility
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo PhilippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

        public static DateTime GetCurrentPhilippineTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhilippineTimeZone);
        }

        public static string GetCurrentPhilippineTimeFormatted(string format = "MM/dd/yyyy hh:mm tt")
        {
            var philippineTime = GetCurrentPhilippineTime();
            return philippineTime.ToString(format);
        }
    }
}