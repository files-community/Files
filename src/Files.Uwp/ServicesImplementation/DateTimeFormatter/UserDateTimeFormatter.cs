using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared.Enums;
using Files.Shared.Services.DateTimeFormatter;
using System;
using Windows.Storage;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    internal class UserDateTimeFormatter : IDateTimeFormatter
    {
        private readonly IDateTimeFormatterFactory factory = Ioc.Default.GetService<IDateTimeFormatterFactory>();
        private readonly ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

        private TimeStyle timeStyle = TimeStyle.Application;
        private IDateTimeFormatter formatter;

        public string Name => formatter.Name;

        public UserDateTimeFormatter()
        {
            timeStyle = GetCurrentTimeStyle();
            formatter = factory.GetDateTimeFormatter(timeStyle);
        }

        public string ToShortLabel(DateTimeOffset offset)
        {
            Update();
            return formatter.ToShortLabel(offset);
        }
        public string ToLongLabel(DateTimeOffset offset)
        {
            Update();
            return formatter.ToLongLabel(offset);
        }

        public ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset)
        {
            Update();
            return formatter.ToTimeSpanLabel(offset);
        }

        private void Update()
        {
            var currentTimeStyle = GetCurrentTimeStyle();
            if (timeStyle != currentTimeStyle)
            {
                timeStyle = currentTimeStyle;
                formatter = factory.GetDateTimeFormatter(timeStyle);
            }
        }

        private TimeStyle GetCurrentTimeStyle()
            => Enum.Parse<TimeStyle>(settings.Values[Constants.LocalSettings.DateTimeFormat].ToString());
    }
}
