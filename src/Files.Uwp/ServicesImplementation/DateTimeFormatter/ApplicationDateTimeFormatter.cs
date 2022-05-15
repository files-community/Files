using Microsoft.Toolkit.Uwp;
using System;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    internal class ApplicationDateTimeFormatter : AbstractDateTimeFormatter
    {
        public override string Name => "Application".GetLocalized();

        public override string ToShortLabel(DateTimeOffset offset)
        {
            if (offset.Year is <= 1601 or >= 9999)
            {
                return " ";
            }

            var elapsed = DateTimeOffset.Now - offset;

            return elapsed switch
            {
                { TotalDays: >= 7 } => offset.ToLocalTime().ToString("D"),
                { TotalDays: >= 2 } => string.Format("DaysAgo".GetLocalized(), elapsed.Days),
                { TotalDays: >= 1 } => string.Format("DayAgo".GetLocalized(), elapsed.Days),
                { TotalHours: >= 2 } => string.Format("HoursAgo".GetLocalized(), elapsed.Hours),
                { TotalHours: >= 1 } => string.Format("HourAgo".GetLocalized(), elapsed.Hours),
                { TotalMinutes: >= 2 } => string.Format("MinutesAgo".GetLocalized(), elapsed.Minutes),
                { TotalMinutes: >= 1 } => string.Format("MinuteAgo".GetLocalized(), elapsed.Minutes),
                _ => string.Format("SecondsAgo".GetLocalized(), elapsed.Seconds),
            };
        }

        public override string ToLongLabel(DateTimeOffset offset)
        {
            var elapsed = DateTimeOffset.Now - offset;

            if (offset.Year is <= 1601 or >= 9999)
            {
                return " ";
            }
            var localTime = offset.ToLocalTime();
            if (elapsed.TotalDays < 7)
            {
                return $"{localTime:D} {localTime:t} ({ToShortLabel(offset)})";
            }
            return $"{localTime:D} {localTime:t}";
        }
    }
}
