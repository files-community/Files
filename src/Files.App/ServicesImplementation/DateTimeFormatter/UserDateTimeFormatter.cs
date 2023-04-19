using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Files.Shared.Services.DateTimeFormatter;

namespace Files.App.ServicesImplementation.DateTimeFormatter
{
	internal class UserDateTimeFormatter : IDateTimeFormatter
	{
		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private IDateTimeFormatter formatter;

		public string Name
			=> formatter.Name;

		public UserDateTimeFormatter()
		{
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

			Update();
		}

		public string ToShortLabel(DateTimeOffset offset)
			=> formatter.ToShortLabel(offset);

		public string ToLongLabel(DateTimeOffset offset)
			=> formatter.ToLongLabel(offset);

		public ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset)
			=> formatter.ToTimeSpanLabel(offset);

		private void Update()
		{
			var dateTimeFormat = UserSettingsService.GeneralSettingsService.DateTimeFormat;
			var factory = Ioc.Default.GetService<IDateTimeFormatterFactory>();

			formatter = factory.GetDateTimeFormatter(dateTimeFormat);
		}

		private void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			if (e.SettingName is nameof(UserSettingsService.GeneralSettingsService.DateTimeFormat))
				Update();
		}
	}
}
