// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	public interface IWindowContext : INotifyPropertyChanged
	{
		bool IsCompactOverlay { get; }

		int TabStripSelectedIndex { get; set; }

		bool IsAppElevated { get; set; }

		bool IsPasteEnabled { get; set; }

		bool IsMainWindowClosed { get; set; }

		int PropertiesWindowCount { get; }

		bool ForceProcessTermination { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the path for Google Drive.
		/// </summary>
		string GoogleDrivePath { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the path for pCloud Drive.
		/// </summary>
		string PCloudDrivePath { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the AppWindow DPI.
		/// </summary>
		/// <remarks>
		/// TODO: update value if the DPI changes
		/// </remarks>
		float AppWindowDPI { get; set; }

		int IncrementPropertiesWindowCount();

		int DecrementPropertiesWindowCount();
	}
}
