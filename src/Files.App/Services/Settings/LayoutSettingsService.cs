// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.Settings
{
	internal sealed partial class LayoutSettingsService : BaseObservableJsonSettings, ILayoutSettingsService
	{
		public LayoutSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}

		public bool SyncFolderPreferencesAcrossDirectories
		{
			get => Get(false);
			set => Set(value);
		}

		public List<LayoutTemplate>? LayoutTemplates
		{
			get => Get<List<LayoutTemplate>>(null);
			set => Set(value);
		}

		public List<LayoutRule>? LayoutRules
		{
			get => Get<List<LayoutRule>>(null);
			set => Set(value);
		}

		public IReadOnlyDictionary<string, JsonElement> TakeLegacySettings(IEnumerable<string> names)
		{
			var settings = JsonSettingsDatabase?.ExportSettings();
			if (settings is null)
				return new Dictionary<string, JsonElement>();

			var values = new Dictionary<string, JsonElement>();
			foreach (var name in names)
			{
				if (settings.TryGetValue(name, out var value))
				{
					values[name] = value.Clone();
					JsonSettingsDatabase!.RemoveKey(name);
				}
			}

			return values;
		}

		public bool AutoSizeColumnsInDetailsLayout
		{
			get => Get(false);
			set => Set(value);
		}

		public DetailsViewSizeKind DetailsViewSize
		{
			get => Get(DetailsViewSizeKind.Small);
			set => Set(value);
		}

		public ListViewSizeKind ListViewSize
		{
			get => Get(ListViewSizeKind.Small);
			set => Set(value);
		}

		public CardsViewSizeKind CardsViewSize
		{
			get => Get(CardsViewSizeKind.Small);
			set => Set(value);
		}

		public GridViewSizeKind GridViewSize
		{
			get => Get(GridViewSizeKind.Large);
			set => Set(value);
		}

		public ColumnsViewSizeKind ColumnsViewSize
		{
			get => Get(ColumnsViewSizeKind.Small);
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
