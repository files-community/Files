using Files.App.Extensions;
using Files.Core.Services.DateTimeFormatter;
using System;
using System.Globalization;
using Windows.Globalization;

namespace Files.App.ServicesImplementation.DateTimeFormatter
{
	internal abstract class AbstractDateTimeFormatter : IDateTimeFormatter
	{
		private static readonly CultureInfo cultureInfo = new(ApplicationLanguages.Languages[0]);

		public abstract string Name { get; }

		public abstract string ToShortLabel(DateTimeOffset offset);

		public virtual string ToLongLabel(DateTimeOffset offset)
			=> ToShortLabel(offset);

		public ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset)
		{
			var now = DateTimeOffset.Now;
			var time = offset.ToLocalTime();

			var diff = now - offset;
			var y = now.AddDays(-1);
			var w = now.AddDays(diff.Days * -1);

			return 0 switch
			{
				_ when now.Date == time.Date => new Label("Today", "\ue184", 7),
				_ when y.Date == time.Date => new Label("ItemTimeText_Yesterday", "\ue161", 6),
				_ when diff.Days < 7 && w.Year == time.Year && GetWeekOfYear(w) == GetWeekOfYear(time) => new Label("ItemTimeText_ThisWeek", "\uE162", 5),
				_ when diff.Days < 14 && w.Year == time.Year && GetWeekOfYear(w) == GetWeekOfYear(time) => new Label("ItemTimeText_LastWeek", "\uE162", 4),
				_ when now.Year == time.Year && now.Month == time.Month => new Label("ItemTimeText_ThisMonth", "\ue163", 3),
				_ when now.AddMonths(-1).Year == time.Year && now.AddMonths(-1).Month == time.Month => new Label("ItemTimeText_LastMonth", "\ue163", 2),
				_ when now.Year == time.Year => new Label("ItemTimeText_ThisYear", "\ue163", 1),
				_ => new Label("ItemTimeText_Older", "\uEC92", 0),
			};
		}

		protected static string ToString(DateTimeOffset offset, string format)
			=> offset.ToLocalTime().ToString(format, cultureInfo);

		private static int GetWeekOfYear(DateTimeOffset t)
		{
			// Should we use the system setting for the first day of week in the future?
			return cultureInfo.Calendar.GetWeekOfYear(t.DateTime, CalendarWeekRule.FirstDay, System.DayOfWeek.Sunday);
		}

		private class Label : ITimeSpanLabel
		{
			public string Text { get; }

			public string Glyph { get; }

			public int Index { get; }

			public Label(string textKey, string glyph, int index)
				=> (Text, Glyph, Index) = (textKey.GetLocalizedResource(), glyph, index);
		}
	}
}
