using Files.Shared.Enums;
using System.Collections.Generic;
using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
	public interface IPreferencesSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the delete confirmation dialog when deleting items.
		/// </summary>
		bool ShowConfirmDeleteDialog { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not media thumbnails should be visible.
		/// </summary>
		bool ShowThumbnails { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to select files and folders when hovering them.
		/// </summary>
		bool SelectFilesOnHover { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not file extensions should be visible.
		/// </summary>
		bool ShowFileExtensions { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to search unindexed items.
		/// </summary>
		bool SearchUnindexedItems { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to navigate to a specific location when launching the app.
		/// </summary>
		bool OpenSpecificPageOnStartup { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default startup location.
		/// </summary>
		string OpenSpecificPageOnStartupPath { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not continue the last session whenever the app is launched.
		/// </summary>
		bool ContinueLastSessionOnStartUp { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to open a page when the app is launched.
		/// </summary>
		bool OpenNewTabOnStartup { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not opening the app from the jumplist should open the directory in a new instance.
		/// </summary>
		bool AlwaysOpenNewInstance { get; set; }

		/// <summary>
		/// A list containing all paths to open at startup.
		/// </summary>
		List<string> TabsOnStartupList { get; set; }

		/// <summary>
		/// A list containing all paths to tabs closed on last session.
		/// </summary>
		List<string> LastSessionTabList { get; set; }

		/// <summary>
		/// Gets or sets a value indicating which date and time format to use.
		/// </summary>
		DateTimeFormats DateTimeFormat { get; set; }
	}
}
