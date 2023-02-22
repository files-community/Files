using System.ComponentModel;

namespace Files.Core.Services.Settings
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		int DefaultGridViewSize { get; set; }
	}
}
