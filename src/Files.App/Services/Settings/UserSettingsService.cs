// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace Files.App.Services.Settings
{
	/// <inheritdoc cref="IUserSettingsService"/>
	internal sealed class UserSettingsService : BaseJsonSettings, IUserSettingsService
	{
		private IGeneralSettingsService? _GeneralSettingsService;
		public IGeneralSettingsService GeneralSettingsService
			=> GetSettingsService(ref _GeneralSettingsService!);

		private IFoldersSettingsService? _FoldersSettingsService;
		public IFoldersSettingsService FoldersSettingsService
			=> GetSettingsService(ref _FoldersSettingsService!);

		private IAppearanceSettingsService? _AppearanceSettingsService;
		public IAppearanceSettingsService AppearanceSettingsService
			=> GetSettingsService(ref _AppearanceSettingsService!);

		private IInfoPaneSettingsService? _InfoPaneSettingsService;
		public IInfoPaneSettingsService InfoPaneSettingsService
			=> GetSettingsService(ref _InfoPaneSettingsService!);

		private ILayoutSettingsService? _LayoutSettingsService;
		public ILayoutSettingsService LayoutSettingsService
			=> GetSettingsService(ref _LayoutSettingsService!);

		private IApplicationSettingsService? _ApplicationSettingsService;
		public IApplicationSettingsService ApplicationSettingsService
			=> GetSettingsService(ref _ApplicationSettingsService!);

		private IAppSettingsService? _AppSettingsService;
		public IAppSettingsService AppSettingsService
			=> GetSettingsService(ref _AppSettingsService!);

		public UserSettingsService()
		{
			JsonSettingsDatabase = Ioc.Default.GetRequiredService<IJsonSettingsDatabaseService>();

			JsonSettingsDatabase.CreateJsonFile(
				Path.Combine(
					ApplicationData.Current.LocalFolder.Path,
					Constants.LocalSettings.SettingsFolderName,
					Constants.LocalSettings.UserSettingsFileName));
		}

		public override object ExportSettings()
		{
			var export = (IDictionary<string, object>)base.ExportSettings();

			// Remove session settings
			export.Remove(nameof(GeneralSettingsService.LastSessionTabList));
			export.Remove(nameof(GeneralSettingsService.LastCrashedTabList));
			export.Remove(nameof(GeneralSettingsService.PathHistoryList));

			return JsonSerializer.Serialize(export, JsonSerializerOptions) ?? string.Empty;
		}

		public override bool ImportSettings(object import)
		{
			Dictionary<string, object> settingsImport = import switch
			{
				string s => JsonSerializer.Deserialize<Dictionary<string, object>>(s) ?? [],
				Dictionary<string, object> d => d,
				_ => [],
			};

			if (!settingsImport.IsEmpty() && base.ImportSettings(settingsImport))
			{
				foreach (var item in settingsImport)
					RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(item.Key, item.Value));

				return true;
			}

			return false;
		}

		private static TSettingsService GetSettingsService<TSettingsService>(ref TSettingsService settingsServiceMember)
			where TSettingsService : class, IBaseSettingsService
		{
			settingsServiceMember ??= Ioc.Default.GetService<TSettingsService>()!;

			return settingsServiceMember;
		}
	}
}
