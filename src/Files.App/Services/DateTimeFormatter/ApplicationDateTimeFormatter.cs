// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Extensions;
using System;

namespace Files.App.Services.DateTimeFormatter
{
	internal sealed class ApplicationDateTimeFormatter : AbstractDateTimeFormatter
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
				{ TotalDays: >= 1 } => "DaysAgo".GetLocalizedFormatResource(elapsed.Days),
				{ TotalHours: >= 1 } => "HoursAgo".GetLocalizedFormatResource(elapsed.Hours),
				{ TotalMinutes: >= 1 } => "MinutesAgo".GetLocalizedFormatResource(elapsed.Minutes),
				{ TotalSeconds: >= 1 } => "SecondsAgo".GetLocalizedFormatResource(elapsed.Seconds),
				{ TotalSeconds: >= 0 } => "Now".GetLocalizedResource(),
				_ => ToString(offset, "D"),
			};
		}

		public override string ToLongLabel(DateTimeOffset offset)
		{
			var elapsed = DateTimeOffset.Now - offset;

			if (offset.Year is <= 1601 or >= 9999)
				return " ";

			if (elapsed.TotalDays < 7 && elapsed.TotalSeconds >= 0)
				return $"{ToString(offset, "D")} {ToString(offset, "t")} ({ToShortLabel(offset)})";

			return $"{ToString(offset, "D")} {ToString(offset, "t")}";
		}
	}
}
