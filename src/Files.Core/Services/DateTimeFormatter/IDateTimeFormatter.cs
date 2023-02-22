using System;

namespace Files.Core.Services.DateTimeFormatter
{
	public interface IDateTimeFormatter
	{
		string Name { get; }

		string ToShortLabel(DateTimeOffset offset);
		string ToLongLabel(DateTimeOffset offset);

		ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset);
	}
}
