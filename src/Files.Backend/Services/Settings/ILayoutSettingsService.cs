using Files.Shared.Enums;
using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		int DefaultGridViewSize { get; set; }

		// move to folder settings and add UI to settings pages
		SortDirection DefaultDirectorySortDirection { get; set; }

		SortOption DefaultDirectorySortOption { get; set; }

		bool DefaultSortDirectoriesAlongsideFiles { get; set; }

		GroupOption DefaultDirectoryGroupOption { get; set; }
	}
}
