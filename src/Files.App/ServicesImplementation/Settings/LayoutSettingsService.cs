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
			get => (int)Get((long)Constants.Browser.GridViewBrowser.GridViewSizeMedium);
			set => Set((long)value);
		}

		public SortDirection DefaultDirectorySortDirection
		{
			get => (SortDirection)Get((long)SortDirection.Ascending);
			set => Set((long)value);
		}

		public SortDirection DefaultDirectoryGroupDirection
		{
			get => (SortDirection)Get((long)SortDirection.Ascending);
			set => Set((long)value);
		}

		public bool DefaultSortDirectoriesAlongsideFiles
		{
			get => Get(false);
			set => Set(value);
		}
	}
}
