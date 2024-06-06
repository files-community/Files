// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.DateTimeFormatter
{
	/// <summary>
	/// Application-specific implementation of the DateTimeFormatter.
	/// </summary>
	internal sealed class ApplicationDateTimeFormatter : AbstractDateTimeFormatter
	{
		/// <summary>
		/// Gets the name of the formatter.
		/// </summary>
		public override string Name
			=> "Application".GetLocalizedFormatResource();

		/// <summary>
		/// Converts the provided <see cref="DateTimeOffset"/> to a short label.
		/// </summary>
		/// <param name="offset">The <see cref="DateTimeOffset"/> to convert.</param>
		/// <returns>The short label representation of the provided <see cref="DateTimeOffset"/>.</returns>
		public override string ToShortLabel(DateTimeOffset offset)
		{
			// Check if the year is out of the valid range
			if (offset.Year is <= 1601 or >= 9999)
				return " ";

			var elapsed = DateTimeOffset.Now - offset;

			// Select label based on elapsed time
			return elapsed switch
			{
				{ TotalDays: >= 7 } => ToString(offset, "D"),
				{ TotalDays: >= 1 } => "DaysAgo".GetLocalizedFormatResource(elapsed.Days),
				{ TotalHours: >= 1 } => "HoursAgo".GetLocalizedFormatResource(elapsed.Hours),
				{ TotalMinutes: >= 1 } => "MinutesAgo".GetLocalizedFormatResource(elapsed.Minutes),
				{ TotalSeconds: >= 1 } => "SecondsAgo".GetLocalizedFormatResource(elapsed.Seconds),
				{ TotalSeconds: >= 0 } => "Now".GetLocalizedFormatResource(),
				_ => ToString(offset, "D"),
			};
		}

		/// <summary>
		/// Converts the provided <see cref="DateTimeOffset"/> to a long label.
		/// </summary>
		/// <param name="offset">The <see cref="DateTimeOffset"/> to convert.</param>
		/// <returns>The long label representation of the provided <see cref="DateTimeOffset"/>.</returns>
		public override string ToLongLabel(DateTimeOffset offset)
		{
			// Check if the year is out of the valid range
			if (offset.Year is <= 1601 or >= 9999)
				return " ";

			var elapsed = DateTimeOffset.Now - offset;
			var date = ToString(offset, "D");
			var time = ToString(offset, "t");

			if (elapsed.TotalDays < 7 && elapsed.TotalSeconds >= 0)
			{
				// Use StringBuilder for efficient string concatenation
				var buffer = new StringBuilder(date.Length + time.Length + 10);
				_ = buffer.Append(date).Append(' ').Append(time).Append(' ').Append(ToShortLabel(offset));
				return buffer.ToString();
			}
			else
			{
				// Use StringBuilder for efficient string concatenation
				var buffer = new StringBuilder(date.Length + time.Length);
				_ = buffer.Append(date).Append(' ').Append(time);
				return buffer.ToString();
			}
		}
	}
}
