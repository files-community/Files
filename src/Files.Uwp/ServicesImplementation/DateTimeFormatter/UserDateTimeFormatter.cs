using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared.Services.DateTimeFormatter;
using Files.Uwp.ViewModels;
using System;
using System.ComponentModel;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    internal class UserDateTimeFormatter : IDateTimeFormatter
    {
        private IDateTimeFormatter formatter;

        public string Name => formatter.Name;

        public UserDateTimeFormatter()
        {
            App.AppSettings.PropertyChanged += AppSettings_PropertyChanged;
            Update();
        }

        public string ToShortLabel(DateTimeOffset offset) => formatter.ToShortLabel(offset);
        public string ToLongLabel(DateTimeOffset offset) => formatter.ToLongLabel(offset);

        public ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset) => formatter.ToTimeSpanLabel(offset);

        private void Update()
        {
            var timeStyle = App.AppSettings.DisplayedTimeStyle;
            var factory = Ioc.Default.GetService<IDateTimeFormatterFactory>();
            formatter = factory.GetDateTimeFormatter(timeStyle);
        }

        private void AppSettings_PropertyChanged(object _, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(SettingsViewModel.DisplayedTimeStyle))
            {
                Update();
            }
        }
    }
}
