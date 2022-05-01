using Files.Shared.Enums;
using Files.Shared.Services.DateTimeFormatter;
using Microsoft.Toolkit.Uwp;
using System;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    public class DateTimeFormatterFactory : IDateTimeFormatterFactory
    {
        public IDateTimeFormatter GetDateTimeFormatter(TimeStyle timeStyle) => timeStyle switch
        {
            TimeStyle.Application => new ApplicationDateTimeFormatter("Application".GetLocalized()),
            TimeStyle.System => new FormatDateTimeFormatter("SystemTimeStyle".GetLocalized(), "g"),
            TimeStyle.Universal => new FormatDateTimeFormatter("Universal".GetLocalized(), "yyyy-MM-dd HH:mm:ss"),
            _ => throw new ArgumentException(nameof(timeStyle)),
        };
    }
}
