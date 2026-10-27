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

		public FolderLayoutModes DefaultLayoutMode
		{
			get => (FolderLayoutModes)Get((long)FolderLayoutModes.Adaptive);
			set => Set((long)value);
		}

		public SortOption DefaultSortOption
		{
			get => (SortOption)Get((long)SortOption.Name);
			set => Set((long)value);
		}

		public string DefaultSortColumnId
		{
			get => Get(string.Empty) ?? string.Empty;
			set => Set(value);
		}

		public SortDirection DefaultDirectorySortDirection
		{
			get => (SortDirection)Get((long)SortDirection.Ascending);
			set => Set((long)value);
		}

		public bool DefaultSortDirectoriesAlongsideFiles
		{
			get => Get(false);
			set => Set(value);
		}

		public bool DefaultSortFilesFirst
		{
			get => Get(false);
			set => Set(value);
		}

		public GroupOption DefaultGroupOption
		{
			get => (GroupOption)Get((long)GroupOption.None);
			set => Set((long)value);
		}

		public string DefaultGroupColumnId
		{
			get => Get(string.Empty) ?? string.Empty;
			set => Set(value);
		}

		public List<string>? DetailsLayoutColumns
		{
			get => Get<List<string>>(null);
			set => Set(value);
		}

		public List<string>? DetailsLayoutColumnWidths
		{
			get => Get<List<string>>(null);
			set => Set(value);
		}

		public SortDirection DefaultDirectoryGroupDirection
		{
			get => (SortDirection)Get((long)SortDirection.Ascending);
			set => Set((long)value);
		}

		public GroupByDateUnit DefaultGroupByDateUnit
		{
			get => (GroupByDateUnit)Get((long)GroupByDateUnit.Year);
			set => Set((long)value);
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
