using System;
using System.Globalization;

namespace WilPick.Helpers
{
    public static class TimeParser
    {
        /// <summary>
        /// Try to extract hour, minute and second from inputs like "11:00:00".
        /// Returns true on success and populates out parameters.
        /// </summary>
        public static bool TryExtractHms(string? input, out int hour, out int minute, out int second)
        {
            hour = minute = second = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var s = input.Trim();

            // Prefer TimeOnly (NET 8) with exact format first
            if (TimeOnly.TryParseExact(s, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tod) ||
                TimeOnly.TryParse(s, CultureInfo.InvariantCulture, out tod))
            {
                hour = tod.Hour;
                minute = tod.Minute;
                second = tod.Second;
                return true;
            }

            // Fallback to TimeSpan
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
            {
                // For TimeSpan, Hours is the hour component within a day (0-23),
                // use TotalHours if you expect values > 24.
                hour = ts.Hours;
                minute = ts.Minutes;
                second = ts.Seconds;
                return true;
            }

            return false;
        }
    }
}