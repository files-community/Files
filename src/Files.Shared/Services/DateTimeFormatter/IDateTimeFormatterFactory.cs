using Files.Shared.Enums;

namespace Files.Shared.Services.DateTimeFormatter
{
    public interface IDateTimeFormatterFactory
    {
        IDateTimeFormatter GetDateTimeFormatter(TimeStyle timeStyle);
    }
}
