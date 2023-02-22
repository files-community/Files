using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Serialization;
using Files.App.Serialization.Implementation;
using Files.Core.Services.Settings;
using Files.Core.EventArguments;
using Files.Core.Extensions;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class UserSettingsService : BaseJsonSettings, IUserSettingsService
	{
		private IPreferencesSettingsService _PreferencesSettingsService;
		public IPreferencesSettingsService PreferencesSettingsService
		{
			get => GetSettingsService(ref _PreferencesSettingsService);
		}

		private IFoldersSettingsService _FoldersSettingsService;
		public IFoldersSettingsService FoldersSettingsService
		{
			get => GetSettingsService(ref _FoldersSettingsService);
		}

		private IAppearanceSettingsService _AppearanceSettingsService;
		public IAppearanceSettingsService AppearanceSettingsService
		{
			get => GetSettingsService(ref _AppearanceSettingsService);
		}

		private IPreviewPaneSettingsService _PreviewPaneSettingsService;
		public IPreviewPaneSettingsService PreviewPaneSettingsService
		{
			get => GetSettingsService(ref _PreviewPaneSettingsService);
		}

		private ILayoutSettingsService _LayoutSettingsService;
		public ILayoutSettingsService LayoutSettingsService
		{
			get => GetSettingsService(ref _LayoutSettingsService);
		}

		private IApplicationSettingsService _ApplicationSettingsService;
		public IApplicationSettingsService ApplicationSettingsService
		{
			get => GetSettingsService(ref _ApplicationSettingsService);
		}

		private IAppSettingsService _AppSettingsService;
		public IAppSettingsService AppSettingsService
		{
			get => GetSettingsService(ref _AppSettingsService);
		}

		public UserSettingsService()
		{
			SettingsSerializer = new DefaultSettingsSerializer();
			JsonSettingsSerializer = new DefaultJsonSettingsSerializer();
			JsonSettingsDatabase = new CachingJsonSettingsDatabase(SettingsSerializer, JsonSettingsSerializer);

			Initialize(Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.UserSettingsFileName));
		}

		public override object ExportSettings()
		{
			var export = (Dictionary<string, object>)base.ExportSettings();

			// Remove session settings
			export.Remove(nameof(PreferencesSettingsService.LastSessionTabList));

			return JsonSettingsSerializer.SerializeToJson(export);
		}

		public override bool ImportSettings(object import)
		{
			Dictionary<string, object> settingsImport = import switch
			{
				string s => JsonSettingsSerializer?.DeserializeFromJson<Dictionary<string, object>>(s) ?? new(),
				Dictionary<string, object> d => d,
				_ => new(),
			};

			if (!settingsImport.IsEmpty() && base.ImportSettings(settingsImport))
			{
				foreach (var item in settingsImport)
				{
					RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(item.Key, item.Value));
				}

				return true;
			}

			return false;
		}

		private TSettingsService GetSettingsService<TSettingsService>(ref TSettingsService settingsServiceMember)
			where TSettingsService : class, IBaseSettingsService
		{
			settingsServiceMember ??= Ioc.Default.GetService<TSettingsService>()!;

			return settingsServiceMember;
		}
	}
}
