using System.Text.Json;
using IBS.DTOs;

namespace IBS.Utility.Helpers
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo PhilippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

        private static readonly HttpClient _httpClient = new();

        public static DateTime GetCurrentPhilippineTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhilippineTimeZone);
        }

        public static string GetCurrentPhilippineTimeFormatted(DateTime dateTime = default, string format = "MM/dd/yyyy hh:mm tt")
        {
            var philippineTime = dateTime != default ? dateTime : GetCurrentPhilippineTime();
            return philippineTime.ToString(format);
        }

        public static async Task<List<DateOnly>> GetNonWorkingDays(DateOnly startDate, DateOnly endDate, string countryCode)
        {
            var nonWorkingDays = new List<DateOnly>();

            var jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var httpClient = new HttpClient();

            // Get holidays for all years in the range
            for (int year = startDate.Year; year <= endDate.Year; year++)
            {
                using var response = await httpClient.GetAsync($"https://date.nager.at/api/v3/publicholidays/{year}/{countryCode}");

                if (response.IsSuccessStatusCode)
                {
                    await using var jsonStream = await response.Content.ReadAsStreamAsync();
                    var items = JsonSerializer.Deserialize<List<PublicHolidayDto>>(jsonStream, jsonSerializerOptions);

                    if (items is not null)
                    {
                        nonWorkingDays.AddRange(
                            items.Select(h => DateOnly.FromDateTime(h.Date))
                        );
                    }
                }
            }

            // Filter holidays within range
            nonWorkingDays = nonWorkingDays.Where(d => d >= startDate && d <= endDate).ToList();

            // Add weekends that are not already holidays
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if ((date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    && !nonWorkingDays.Contains(date))
                {
                    nonWorkingDays.Add(date);
                }
            }

            nonWorkingDays.Sort();

            return nonWorkingDays;
        }
    }
}
