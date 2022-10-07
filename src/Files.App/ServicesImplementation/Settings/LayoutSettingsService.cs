using Files.App.Serialization;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class LayoutSettingsService : BaseObservableJsonSettings, ILayoutSettingsService
	{
		public LayoutSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}		

		public int DefaultGridViewSize
		{
			get => (int)Get((long)Constants.Browser.GridViewBrowser.GridViewSizeSmall);
			set => Set((long)value);
		}

		public SortDirection DefaultDirectorySortDirection
		{
			get => (SortDirection)Get((long)SortDirection.Ascending);
			set => Set((long)value);
		}

		public SortOption DefaultDirectorySortOption
		{
			get => (SortOption)Get((long)SortOption.Name);
			set => Set((long)value);
		}
		
		public bool DefaultSortDirectoriesAlongsideFiles
		{
			get => Get(false);
			set => Set(value);
		}

		public GroupOption DefaultDirectoryGroupOption
		{
			get => (GroupOption)Get((long)GroupOption.None);
			set => Set((long)value);
		}
	}
}
