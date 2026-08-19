// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Serialization.Implementation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections.Concurrent;
using System.IO;
using Windows.Storage;

namespace Files.App.Services.Settings
{
	[JsonSourceGenerationOptions(WriteIndented = true)]
	[JsonSerializable(typeof(object))]
	[JsonSerializable(typeof(ConcurrentDictionary<string, JsonElement>))]
	[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
	[JsonSerializable(typeof(bool))]
	[JsonSerializable(typeof(string))]
	[JsonSerializable(typeof(int))]
	[JsonSerializable(typeof(long))]
	[JsonSerializable(typeof(double))]
	[JsonSerializable(typeof(float))]
	[JsonSerializable(typeof(List<string>))]
	[JsonSerializable(typeof(Dictionary<string, bool>))]
	[JsonSerializable(typeof(List<ActionWithParameterItem>))]
	[JsonSerializable(typeof(Dictionary<string, List<ToolbarItemSettingsEntry>>))]
	[JsonSerializable(typeof(Dictionary<string, List<string>>))]
	[JsonSerializable(typeof(DateTimeFormats))]
	[JsonSerializable(typeof(SingleClickOpenMode))]
	[JsonSerializable(typeof(SizeUnitTypes))]
	[JsonSerializable(typeof(OpenInIDEOption))]
	[JsonSerializable(typeof(BackdropMaterialType))]
	[JsonSerializable(typeof(Stretch))]
	[JsonSerializable(typeof(VerticalAlignment))]
	[JsonSerializable(typeof(HorizontalAlignment))]
	[JsonSerializable(typeof(StatusCenterVisibility))]
	[JsonSerializable(typeof(InfoPaneTabs))]
	[JsonSerializable(typeof(DetailsViewSizeKind))]
	[JsonSerializable(typeof(ListViewSizeKind))]
	[JsonSerializable(typeof(CardsViewSizeKind))]
	[JsonSerializable(typeof(GridViewSizeKind))]
	[JsonSerializable(typeof(ColumnsViewSizeKind))]
	internal sealed partial class UserSettingsJsonSerializationContext : JsonSerializerContext
	{
	}

	internal sealed class UserSettingsService : BaseJsonSettings, IUserSettingsService
	{
		private IGeneralSettingsService? _GeneralSettingsService;
		public IGeneralSettingsService GeneralSettingsService
		{
			get => GetSettingsService(ref _GeneralSettingsService);
		}

		private IFoldersSettingsService? _FoldersSettingsService;
		public IFoldersSettingsService FoldersSettingsService
		{
			get => GetSettingsService(ref _FoldersSettingsService);
		}

		private IAppearanceSettingsService? _AppearanceSettingsService;
		public IAppearanceSettingsService AppearanceSettingsService
		{
			get => GetSettingsService(ref _AppearanceSettingsService);
		}

		private IInfoPaneSettingsService? _InfoPaneSettingsService;
		public IInfoPaneSettingsService InfoPaneSettingsService
		{
			get => GetSettingsService(ref _InfoPaneSettingsService);
		}

		private ILayoutSettingsService? _LayoutSettingsService;
		public ILayoutSettingsService LayoutSettingsService
		{
			get => GetSettingsService(ref _LayoutSettingsService);
		}

		private IApplicationSettingsService? _ApplicationSettingsService;
		public IApplicationSettingsService ApplicationSettingsService
		{
			get => GetSettingsService(ref _ApplicationSettingsService);
		}

		private IAppSettingsService? _AppSettingsService;
		public IAppSettingsService AppSettingsService
		{
			get => GetSettingsService(ref _AppSettingsService);
		}

		public UserSettingsService()
		{
			var settingsSerializer = new DefaultSettingsSerializer();
			SettingsSerializer = settingsSerializer;

			Initialize(Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.UserSettingsFileName));

			var jsonSettingsSerializer = new DefaultJsonSettingsSerializer();
			JsonSettingsSerializer = jsonSettingsSerializer;
			JsonSettingsDatabase = new CachingJsonSettingsDatabase(
				settingsSerializer,
				jsonSettingsSerializer,
				UserSettingsJsonSerializationContext.Default);
		}

		public override object ExportSettings()
		{
			var export = new Dictionary<string, JsonElement>((IDictionary<string, JsonElement>)base.ExportSettings());

			// Remove session settings
			export.Remove(nameof(GeneralSettingsService.LastSessionTabList));
			export.Remove(nameof(GeneralSettingsService.LastSessionSelectedTabIndex));
			export.Remove(nameof(GeneralSettingsService.LastCrashedTabList));
			export.Remove(nameof(GeneralSettingsService.PathHistoryList));
			export.Remove(nameof(GeneralSettingsService.PreviousSearchQueriesList));
			export.Remove(nameof(GeneralSettingsService.PreviousArchiveExtractionLocations));

			return JsonSettingsSerializer!.SerializeToJson(export, UserSettingsJsonSerializationContext.Default.DictionaryStringJsonElement);
		}

		public override bool ImportSettings(object import)
		{
			IDictionary<string, object?> settingsImport = import switch
			{
				string s => (JsonSettingsSerializer?.DeserializeFromJson(s, UserSettingsJsonSerializationContext.Default.DictionaryStringJsonElement) ?? [])
					.ToDictionary(x => x.Key, x => (object?)x.Value),
				IDictionary<string, JsonElement> d => d.ToDictionary(x => x.Key, x => (object?)x.Value),
				IDictionary<string, object?> d => d,
				_ => new Dictionary<string, object?>(),
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

		private static TSettingsService GetSettingsService<TSettingsService>(ref TSettingsService? settingsServiceMember)
			where TSettingsService : class, IBaseSettingsService
		{
			settingsServiceMember ??= Ioc.Default.GetRequiredService<TSettingsService>();

			return settingsServiceMember;
		}
	}
}
