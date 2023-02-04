using Files.Shared.Enums;
using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		int DefaultGridViewSize { get; set; }

		SortDirection DefaultDirectorySortDirection { get; set; }

		SortDirection DefaultDirectoryGroupDirection { get; set; }

		bool DefaultSortDirectoriesAlongsideFiles { get; set; }
	}
}
