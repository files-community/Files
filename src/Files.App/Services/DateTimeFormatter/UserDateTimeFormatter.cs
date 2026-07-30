// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.DateTimeFormatter
{
	internal sealed class UserDateTimeFormatter : IDateTimeFormatter
	{
		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private IDateTimeFormatter formatter;

		public string Name
			=> formatter.Name;

		public UserDateTimeFormatter()
		{
			formatter = GetFormatter();
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
		}

		public string ToShortLabel(DateTimeOffset offset)
			=> formatter.ToShortLabel(offset);

		public string ToLongLabel(DateTimeOffset offset)
			=> formatter.ToLongLabel(offset);

		public ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset, GroupByDateUnit unit)
			=> formatter.ToTimeSpanLabel(offset, unit);

		private void Update()
			=> formatter = GetFormatter();

		private IDateTimeFormatter GetFormatter()
		{
			var dateTimeFormat = UserSettingsService.GeneralSettingsService.DateTimeFormat;
			var factory = Ioc.Default.GetRequiredService<IDateTimeFormatterFactory>();

			return factory.GetDateTimeFormatter(dateTimeFormat);
		}

		private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
		{
			if (e.SettingName is nameof(UserSettingsService.GeneralSettingsService.DateTimeFormat))
				Update();
		}
	}
}
