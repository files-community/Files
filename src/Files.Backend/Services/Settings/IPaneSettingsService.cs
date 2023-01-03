using Files.Shared.Enums;
using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
	public interface IPreviewPaneSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		bool IsEnabled { get; set; }

		double HorizontalSizePx { get; set; }

		double VerticalSizePx { get; set; }

		double MediaVolume { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if the preview pane should only show the item preview without the details section
		/// </summary>
		bool ShowPreviewOnly { get; set; }
	}
}
