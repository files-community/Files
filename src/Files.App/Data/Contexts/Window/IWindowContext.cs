// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	/// <summary>
	/// Represents context for <see cref="MainWindow"/> comprehensive management.
	/// </summary>
	public interface IWindowContext : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the value that indicates whether the window is in Compact Overlay.
		/// </summary>
		/// <remarks>
		/// This feature comes from Windows, visit
		/// <a href="https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing#compact-overlay">Compact Overlay in WinAppSdk</a>.
		/// </remarks>
		bool IsCompactOverlay { get; }

		/// <summary>
		/// Gets or sets the index of the current selected <see cref="TabBarItem"/>.
		/// </summary>
		int TabBarSelectedItemIndex { get; set; }

		/// <summary>
		/// Gets or sets the value that indicates whether the application process is elevated.
		/// </summary>
		bool IsAppElevated { get; set; }

		/// <summary>
		/// Gets or sets the value that indicates whether the paste file filesystem operation is enabled.
		/// </summary>
		bool IsPasteEnabled { get; set; }

		/// <summary>
		/// Gets or sets the value that indicates the application window is closed.
		/// </summary>
		bool IsMainWindowClosed { get; set; }

		/// <summary>
		/// Gets or sets the count of properties windows that are being opened
		/// </summary>
		int PropertiesWindowCount { get; }

		/// <summary>
		/// Gets or sets the value that indicates whether the application must be terminated on closed.
		/// </summary>
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

		/// <summary>
		/// Increments the count of properties windows that are being opened.
		/// </summary>
		/// <remarks>
		/// Call this make sure the property <see cref="PropertiesWindowCount"/>'s setter is thread-safe.
		/// </remarks>
		/// <returns>The updated count of properties window that are being opened.</returns>
		int IncrementPropertiesWindowCount();

		/// <summary>
		/// Decreases the count of properties windows that are being opened.
		/// </summary>
		/// <remarks>
		/// Call this make sure the property <see cref="PropertiesWindowCount"/>'s setter is thread-safe.
		/// </remarks>
		/// <returns>The updated count of properties window that are being opened.</returns>
		int DecrementPropertiesWindowCount();
	}
}
