using Files.Core.Enums;

namespace Files.Core.Services.DateTimeFormatter
{
	public interface IDateTimeFormatterFactory
	{
		IDateTimeFormatter GetDateTimeFormatter(DateTimeFormats dateTimeFormat);
	}
}
