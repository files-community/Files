using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Extensions
{
    public static class DateTimeExtensions
    {
        public static string GetFriendlyDateFromFormat(this DateTimeOffset d, string returnFormat)
        {
            var elapsed = DateTimeOffset.Now - d;

            if (elapsed.TotalDays > 7 || returnFormat == "g")
            {
                return d.ToLocalTime().ToString(returnFormat);
            }
            else if (elapsed.TotalDays > 2)
            {
                return string.Format("DaysAgo".GetLocalized(), elapsed.Days);
            }
            else if (elapsed.TotalDays > 1)
            {
                return string.Format("DayAgo".GetLocalized(), elapsed.Days);
            }
            else if (elapsed.TotalHours > 2)
            {
                return string.Format("HoursAgo".GetLocalized(), elapsed.Hours);
            }
            else if (elapsed.TotalHours > 1)
            {
                return string.Format("HoursAgo".GetLocalized(), elapsed.Hours);
            }
            else if (elapsed.TotalMinutes > 2)
            {
                return string.Format("MinutesAgo".GetLocalized(), elapsed.Minutes);
            }
            else if (elapsed.TotalMinutes > 1)
            {
                return string.Format("MinutesAgo".GetLocalized(), elapsed.Minutes);
            }
            else
            {
                return string.Format("SecondsAgo".GetLocalized(), elapsed.Seconds);
            }
        }

        /// <summary>
        /// Gets user friendly text representing the time difference between the current time and the specified time
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>User friendly strings representing the text and period and an icon glyph to display</returns>
        public static (string text, string range, string glyph, int index) GetFriendlyTimeSpan(this DateTimeOffset dt)
        {
            Windows.Globalization.Calendar cal = new Windows.Globalization.Calendar();
            var t = DateTimeOffset.Now;
            var t2 = dt.ToLocalTime();
            var today = DateTime.Today;
            
            var diff = t - dt;
            if (t.Month == t2.Month && t.Day == t2.Day)
            {
                return ("Today", today.ToShortDateString(), "\ue184", 0);
            }
            if(t.Month == t2.Month && t.Day - t2.Day < 2)
            {
                return ("Yesterday", today.Subtract(TimeSpan.FromDays(1)).ToShortDateString(), "\ue161", 1);
            }

            // TODO: use system setting for first day of week
            if (diff.Days <= 7 && t.GetWeekOfYear() == t2.GetWeekOfYear())
            {
                return ("Earlier this week", today.GetFriendlyRange(TimeSpan.FromDays((int)t.DayOfWeek)), "\uE162", 2);
            }
            
            if(diff.Days <= 14 && t.GetWeekOfYear() - 1 == t2.GetWeekOfYear())
            {
                return ("Last week", today.GetFriendlyRange(TimeSpan.FromDays(7 + (int)t.DayOfWeek)), "\uE162", 3);
            }

            if(t.Year == t2.Year && t.Month == t2.Month)
            {
                return ("This month", today.GetFriendlyRange(TimeSpan.FromDays(t.Day)), "\ue163", 4);
            }

            if(t.Year == t2.Year)
            {
                return ("This year", today.GetFriendlyRange(TimeSpan.FromDays(t.DayOfYear)), "\ue163", 5);
            }

            return ("Older", $"Before {today.Subtract(TimeSpan.FromDays(today.DayOfYear)).ToShortDateString()}", "\uEC92", 6);
        }

        public static string GetFriendlyRange(this DateTime t, TimeSpan diff)
        {
            return $"{t.Subtract(diff).ToShortDateString()} - {t.ToShortDateString()}";
        }

        private static Calendar calendar;
        public static int GetWeekOfYear(this DateTimeOffset t)
        {
            calendar ??= new CultureInfo(CultureInfo.CurrentUICulture.Name).Calendar;
            // TODO: use system setting for first day of week
            return calendar.GetWeekOfYear(t.DateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        }
    }
}
