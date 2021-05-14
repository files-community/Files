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
                return ("ItemTimeText_Today".GetLocalized(), today.ToUserDateString(), "\ue184", 0);
            }

            if(t.Month == t2.Month && t.Day - t2.Day < 2)
            {
                return ("ItemTimeText_Yesterday".GetLocalized(), today.Subtract(TimeSpan.FromDays(1)).ToUserDateString(), "\ue161", 1);
            }

            if (diff.Days <= 7 && t.GetWeekOfYear() == t2.GetWeekOfYear())
            {
                return ("ItemTimeText_ThisWeek".GetLocalized(), t.Subtract(TimeSpan.FromDays((int)t.DayOfWeek)).ToUserDateString(), "\uE162", 2);
            }

            if (diff.Days <= 14 && t.GetWeekOfYear() - 1 == t2.GetWeekOfYear())
            {
                return ("ItemTimeText_LastWeek".GetLocalized(), t.Subtract(TimeSpan.FromDays((int)t.DayOfWeek + 7)).Date.ToShortDateString(), "\uE162", 3);
            }

            if (t.Year == t2.Year && t.Month == t2.Month)
            {
                return ("ItemTimeText_ThisMonth".GetLocalized(), t.Subtract(TimeSpan.FromDays(t.Day-1)).ToUserDateString(), "\ue163", 4);
            }
            
            if(t.Year == t2.Year && t.Month-1 == t2.Month)
            {
                return ("ItemTimeText_LastMonth".GetLocalized(), t.Subtract(TimeSpan.FromDays(t.Day - 1 + calendar.GetDaysInMonth(t.Year, t.Month-1))).ToUserDateString(), "\ue163", 5);
            }

            if(t.Year == t2.Year)
            {
                return ("ItemTimeText_ThisYear".GetLocalized(), t.Subtract(TimeSpan.FromDays(t.DayOfYear-1)).ToUserDateString(), "\ue163", 5);
            }

            return ("ItemTimeText_Older".GetLocalized(), string.Format("ItemTimeText_Before".GetLocalized(), today.Subtract(TimeSpan.FromDays(today.DayOfYear - 1)).ToUserDateString()), "\uEC92", 6);
        }

        public static (string text, string range, string glyph, int index) GetUserSettingsFriendlyTimeSpan(this DateTimeOffset dt)
        {
            var result = dt.GetFriendlyTimeSpan();
            if (App.AppSettings.DisplayedTimeStyle == Enums.TimeStyle.Application)
            {
                return result;
            }

            return (result.range, result.range, result.glyph, result.index);
        }

        private static Calendar calendar = new CultureInfo(CultureInfo.CurrentUICulture.Name).Calendar;
        public static int GetWeekOfYear(this DateTimeOffset t)
        {
            // Should we use the system setting for the first day of week in the future?
            return calendar.GetWeekOfYear(t.DateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        }

        public static string ToUserDateString(this DateTimeOffset t)
        {
            return t.Date.ToShortDateString();
        }
        
        public static string ToUserDateString(this DateTime t)
        {
            return t.ToShortDateString();
        }
    }
}
