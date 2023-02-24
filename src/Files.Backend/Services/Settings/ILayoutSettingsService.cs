using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		int DefaultGridViewSize { get; set; }
	}
}
