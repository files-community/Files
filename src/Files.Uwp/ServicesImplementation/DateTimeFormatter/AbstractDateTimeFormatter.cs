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

        public ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset)
        {
            var now = DateTimeOffset.Now;
            var time = offset.ToLocalTime();

            var diff = now - offset;
            var y = now.AddDays(-1);
            var w = now.AddDays(diff.Days * -1);

            return 0 switch
            {
                _ when now.Date == time.Date => new Label("Today", "\ue184", 0),
                _ when y.Date == time.Date => new Label("ItemTimeText_Yesterday", "\ue161", 1),
                _ when diff.Days < 7 && w.Year == time.Year && GetWeekOfYear(w) == GetWeekOfYear(time) => new Label("ItemTimeText_ThisWeek", "\uE162", 2),
                _ when diff.Days < 14 && w.Year == time.Year && GetWeekOfYear(w) == GetWeekOfYear(time) => new Label("ItemTimeText_LastWeek", "\uE162", 3),
                _ when now.Year == time.Year && now.Month == time.Month => new Label("ItemTimeText_ThisMonth", "\ue163", 4),
                _ when now.AddMonths(-1).Year == time.Year && now.AddMonths(-1).Month == time.Month => new Label("ItemTimeText_LastMonth", "\ue163", 5),
                _ when now.Year == time.Year => new Label("ItemTimeText_ThisYear", "\ue163", 5),
                _ => new Label("ItemTimeText_Older", "\uEC92", 6),
            };
        }

        private int GetWeekOfYear(DateTimeOffset t)
        {
            // Should we use the system setting for the first day of week in the future?
            return calendar.GetWeekOfYear(t.DateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        }

        private class Label : ITimeSpanLabel
        {
            public string Text { get; }
            public string Glyph { get; }
            public int Index { get; }

            public Label(string textKey, string glyph, int index)
                => (Text, Glyph, Index) = (textKey.GetLocalized(), glyph, index);
        }
    }
}
