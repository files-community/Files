// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Services.DateTimeFormatter
{
	public interface IDateTimeFormatter
	{
		string Name { get; }

		string ToShortLabel(DateTimeOffset offset);
		string ToLongLabel(DateTimeOffset offset);

		ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset, GroupByDateUnit unit);
	}
}
