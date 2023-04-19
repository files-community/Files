using Files.Shared.Enums;
using Files.Shared.Services.DateTimeFormatter;

namespace Files.App.ServicesImplementation.DateTimeFormatter
{
	public class DateTimeFormatterFactory : IDateTimeFormatterFactory
	{
		public IDateTimeFormatter GetDateTimeFormatter(DateTimeFormats dateTimeFormat) => dateTimeFormat switch
		{
			DateTimeFormats.Application => new ApplicationDateTimeFormatter(),
			DateTimeFormats.System => new SystemDateTimeFormatter(),
			DateTimeFormats.Universal => new UniversalDateTimeFormatter(),
			_ => throw new ArgumentException(nameof(dateTimeFormat)),
		};
	}
}
