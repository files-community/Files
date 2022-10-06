﻿using Files.Shared.Enums;
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
		/// Gets or sets a value indicating whether or not to open folders in new tab.
		/// </summary>
		bool OpenFoldersInNewTab { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not file extensions should be visible.
		/// </summary>
		bool ShowFileExtensions { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not media thumbnails should be visible.
		/// </summary>
		bool ShowThumbnails { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not hidden items should be visible.
		/// </summary>
		bool AreHiddenItemsVisible { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not system items should be visible.
		/// </summary>
		bool AreSystemItemsHidden { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not alternate data streams should be visible.
		/// </summary>
		bool AreAlternateStreamsVisible { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to display dot files.
		/// </summary>
		bool ShowDotFiles{ get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to select files and folders when hovering them.
		/// </summary>
		bool SelectFilesOnHover { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not files should open with one click.
		/// </summary>
		bool OpenFilesWithOneClick { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not folders should open with one click.
		/// </summary>
		bool OpenFoldersWithOneClick { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not folders should open with two clicks in ColumnsLayout.
		/// </summary>
		bool ColumnLayoutOpenFoldersWithOneClick { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to search unindexed items.
		/// </summary>
		bool SearchUnindexedItems { get; set; }

		/// <summary>
		/// Forces default directory preferences on all folders
		/// </summary>
		bool ForceLayoutPreferencesOnAllDirectories { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to show folder size.
		/// </summary>
		bool ShowFolderSize { get; set; }

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

		/// <summary>
		/// Gets or sets a value indicating whether or not the date column should be visible by default.
		/// </summary>
		bool ShowDateColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the date created column should be visible by default.
		/// </summary>
		bool ShowDateCreatedColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the type column should be visible by default.
		/// </summary>
		bool ShowTypeColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the size column should be visible by default.
		/// </summary>
		bool ShowSizeColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the filetag column should be visible by default.
		/// </summary>
		bool ShowFileTagColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default layout mode.
		/// </summary>
		FolderLayoutModes DefaultLayoutMode { get; set; }

		/// <summary>
		/// Gets or sets a value indicating tags column's default width
		/// </summary>
		double TagColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating name column's default width
		/// </summary>
		double NameColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating date modified column's default width
		/// </summary>
		double DateModifiedColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating item type column's default width
		/// </summary>
		double TypeColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating date created column's default width
		/// </summary>
		double DateCreatedColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating size column's default width
		/// </summary>
		double SizeColumnWidth { get; set; }
	}
}
