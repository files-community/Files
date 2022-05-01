using Files.Shared.Services.DateTimeFormatter;
using Microsoft.Toolkit.Uwp;
using System;
using System.Globalization;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    internal abstract class AbstractDateTimeFormatter : IDateTimeFormatter
    {
        private readonly Calendar calendar = new CultureInfo(CultureInfo.CurrentUICulture.Name).Calendar;

        public abstract string Name { get; }

        public abstract string ToShortLabel(DateTimeOffset offset);
        public virtual string ToLongLabel(DateTimeOffset offset) => ToShortLabel(offset);

        public virtual ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset)
        {
            var t = DateTimeOffset.Now;
            var t2 = offset.ToLocalTime();
            var today = DateTime.Today;

            var diff = t - offset;
            var y = t.AddDays(-1);
            var w = t.AddDays(diff.Days * -1);

            if (t.Date == t2.Date)
            {
                return new TimeSpanLabel("Today".GetLocalized(), ToRangeLabel(today), "\ue184", 0);
            }
            if (y.Date == t2.Date)
            {
                return new TimeSpanLabel("ItemTimeText_Yesterday".GetLocalized(), ToRangeLabel(today.Subtract(TimeSpan.FromDays(1))), "\ue161", 1);
            }
            if (w.Year == t2.Year && GetWeekOfYear(w) == GetWeekOfYear(t2))
            {
                if (diff.Days < 7)
                {
                    return new TimeSpanLabel("ItemTimeText_ThisWeek".GetLocalized(),
                        ToRangeLabel(t.Subtract(TimeSpan.FromDays((int)t.DayOfWeek)).DateTime), "\uE162", 2);
                }
                if (diff.Days < 14)
                {
                    return new TimeSpanLabel("ItemTimeText_LastWeek".GetLocalized(),
                        ToRangeLabel(t.Subtract(TimeSpan.FromDays((int)t.DayOfWeek + 7)).DateTime), "\uE162", 3);
                }
            }
            if (t.Year == t2.Year && t.Month == t2.Month)
            {
                return new TimeSpanLabel("ItemTimeText_ThisMonth".GetLocalized(), ToRangeLabel(t.Subtract(TimeSpan.FromDays(t.Day - 1)).DateTime), "\ue163", 4);
            }
            if (t.AddMonths(-1).Year == t2.Year && t.AddMonths(-1).Month == t2.Month)
            {
                return new TimeSpanLabel("ItemTimeText_LastMonth".GetLocalized(),
                    ToRangeLabel(t.Subtract(TimeSpan.FromDays(t.Day - 1 + calendar.GetDaysInMonth(t.AddMonths(-1).Year, t.AddMonths(-1).Month))).DateTime), "\ue163", 5);
            }
            if (t.Year == t2.Year)
            {
                return new TimeSpanLabel("ItemTimeText_ThisYear".GetLocalized(), ToRangeLabel(t.Subtract(TimeSpan.FromDays(t.DayOfYear - 1)).DateTime), "\ue163", 5);
            }
            return new TimeSpanLabel("ItemTimeText_Older".GetLocalized(),
                string.Format("ItemTimeText_Before".GetLocalized(), ToRangeLabel(today.Subtract(TimeSpan.FromDays(today.DayOfYear - 1)))), "\uEC92", 6);
        }

        protected virtual string ToRangeLabel(DateTime range) => range.ToShortDateString();

        private int GetWeekOfYear(DateTimeOffset t)
        {
            // Should we use the system setting for the first day of week in the future?
            return calendar.GetWeekOfYear(t.DateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        }

        internal class TimeSpanLabel : ITimeSpanLabel
        {
            public string Text { get; }
            public string Range { get; }
            public string Glyph { get; }
            public int Index { get; }

            public TimeSpanLabel(string text, string range, string glyph, int index)
                => (Text, Range, Glyph, Index) = (text, range, glyph, index);

            public void Deconstruct(out string text, out string range, out string glyph, out int index)
            {
                text = Text;
                range = Range;
                glyph = Glyph;
                index = Index;
            }
        }
    }
}
