namespace Files.App.ServicesImplementation.DateTimeFormatter
{
	internal class ApplicationDateTimeFormatter : AbstractDateTimeFormatter
	{
		public override string Name
			=> "Application".GetLocalizedResource();

		public override string ToShortLabel(DateTimeOffset offset)
		{
			if (offset.Year is <= 1601 or >= 9999)
			{
				return " ";
			}

			var elapsed = DateTimeOffset.Now - offset;

			return elapsed switch
			{
				{ TotalDays: >= 7 } => ToString(offset, "D"),
				{ TotalDays: >= 2 } => string.Format("DaysAgo".GetLocalizedResource(), elapsed.Days),
				{ TotalDays: >= 1 } => string.Format("DayAgo".GetLocalizedResource(), elapsed.Days),
				{ TotalHours: >= 2 } => string.Format("HoursAgo".GetLocalizedResource(), elapsed.Hours),
				{ TotalHours: >= 1 } => string.Format("HourAgo".GetLocalizedResource(), elapsed.Hours),
				{ TotalMinutes: >= 2 } => string.Format("MinutesAgo".GetLocalizedResource(), elapsed.Minutes),
				{ TotalMinutes: >= 1 } => string.Format("MinuteAgo".GetLocalizedResource(), elapsed.Minutes),
				{ TotalSeconds: >= 2 } => string.Format("SecondsAgo".GetLocalizedResource(), elapsed.Seconds),
				{ TotalSeconds: >= 1 } => "OneSecondAgo".GetLocalizedResource(),
				{ TotalSeconds: >= 0 } => "Now".GetLocalizedResource(),
				_ => ToString(offset, "D"),
			};
		}

		public override string ToLongLabel(DateTimeOffset offset)
		{
			var elapsed = DateTimeOffset.Now - offset;

			if (offset.Year is <= 1601 or >= 9999)
				return " ";

			var localTime = offset.ToLocalTime();

			if (elapsed.TotalDays < 7 && elapsed.TotalSeconds >= 0)
				return $"{localTime:D} {localTime:t} ({ToShortLabel(offset)})";

			return $"{localTime:D} {localTime:t}";
		}
	}
}
