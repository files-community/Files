using Files.Shared.Enums;
using Files.Shared.Services.DateTimeFormatter;
using System;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    public class DateTimeFormatterFactory : IDateTimeFormatterFactory
    {
        public IDateTimeFormatter GetDateTimeFormatter(TimeStyle timeStyle) => timeStyle switch
        {
            TimeStyle.Application => new ApplicationDateTimeFormatter(),
            TimeStyle.System => new SystemDateTimeFormatter(),
            TimeStyle.Universal => new UniversalDateTimeFormatter(),
            _ => throw new ArgumentException(nameof(timeStyle)),
        };
    }
}
