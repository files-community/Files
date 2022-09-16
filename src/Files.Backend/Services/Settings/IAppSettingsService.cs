using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
	public interface IAppSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the StatusCenter teaching tip.
		/// </summary>
		bool ShowStatusCenterTeachingTip { get; set; }       
	}
}
