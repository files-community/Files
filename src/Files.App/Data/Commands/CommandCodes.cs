// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Commands
{
	public enum CommandCodes
	{
		None,

		// Global
		OpenHelp,
		ToggleFullScreen,
		EnterCompactOverlay,
		ExitCompactOverlay,
		ToggleCompactOverlay,
		Search,
		SearchUnindexedItems,
		EditPath,
		Redo,
		Undo,

		// Show
		ToggleShowHiddenItems,
		ToggleShowFileExtensions,
		TogglePreviewPane,
		ToggleDetailsPane,
		ToggleInfoPane,

		// File System
		CopyItem,
		CopyPath,
		CopyPathWithQuotes,
		CutItem,
		PasteItem,
		PasteItemToSelection,
		DeleteItem,
		DeleteItemPermanently,
		CreateFolder,
		CreateFolderWithSelection,
		AddItem,
		CreateShortcut,
		CreateShortcutFromDialog,
		EmptyRecycleBin,
		FormatDrive,
		RestoreRecycleBin,
		RestoreAllRecycleBin,
		OpenItem,
		OpenItemWithApplicationPicker,
		OpenParentFolder,
		OpenFileLocation,
		RefreshItems,
		Rename,

		// Selection
		SelectAll,
		InvertSelection,
		ClearSelection,
		ToggleSelect,

		// Share
		ShareItem,

		// Start
		PinToStart,
		UnpinFromStart,

		// Favorites
		PinItemToFavorites,
		UnpinItemFromFavorites,

		// Backgrounds
		SetAsWallpaperBackground,
		SetAsSlideshowBackground,
		SetAsLockscreenBackground,

		// Install
		InstallFont,
		InstallInfDriver,
		InstallCertificate,

		// Run
		RunAsAdmin,
		RunAsAnotherUser,
		RunWithPowershell,

		// Preview Popup
		LaunchPreviewPopup,

		// Archives
		CompressIntoArchive,
		CompressIntoSevenZip,
		CompressIntoZip,
		DecompressArchive,
		DecompressArchiveHere,
		DecompressArchiveToChildFolder,

		// Image Manipulation
		RotateLeft,
		RotateRight,

		// Open
		OpenInVSCode,
		OpenRepoInVSCode,
		OpenProperties,
		OpenSettings,
		OpenTerminal,
		OpenTerminalAsAdmin,
		OpenCommandPalette,

		// Layout
		LayoutDecreaseSize,
		LayoutIncreaseSize,
		LayoutDetails,
		LayoutTiles,
		LayoutGridSmall,
		LayoutGridMedium,
		LayoutGridLarge,
		LayoutColumns,
		LayoutAdaptive,

		// Sort by
		SortByName,
		SortByDateModified,
		SortByDateCreated,
		SortBySize,
		SortByType,
		SortBySyncStatus,
		SortByTag,
		SortByPath,
		SortByOriginalFolder,
		SortByDateDeleted,
		SortAscending,
		SortDescending,
		ToggleSortDirection,
		ToggleSortDirectoriesAlongsideFiles,

		// Group by
		GroupByNone,
		GroupByName,
		GroupByDateModified,
		GroupByDateCreated,
		GroupBySize,
		GroupByType,
		GroupBySyncStatus,
		GroupByTag,
		GroupByOriginalFolder,
		GroupByDateDeleted,
		GroupByFolderPath,
		GroupByDateModifiedYear,
		GroupByDateModifiedMonth,
		GroupByDateCreatedYear,
		GroupByDateCreatedMonth,
		GroupByDateDeletedYear,
		GroupByDateDeletedMonth,
		GroupAscending,
		GroupDescending,
		ToggleGroupDirection,
		GroupByYear,
		GroupByMonth,
		ToggleGroupByDateUnit,

		// Navigation
		NewTab,
		NavigateBack,
		NavigateForward,
		NavigateUp,

		// Other
		DuplicateCurrentTab,
		DuplicateSelectedTab,
		CloseTabsToTheLeftCurrent,
		CloseTabsToTheLeftSelected,
		CloseTabsToTheRightCurrent,
		CloseTabsToTheRightSelected,
		CloseOtherTabsCurrent,
		CloseOtherTabsSelected,
		OpenDirectoryInNewPane,
		OpenDirectoryInNewTab,
		OpenInNewWindowItem,
		ReopenClosedTab,
		PreviousTab,
		NextTab,
		CloseSelectedTab,
		OpenNewPane,
		ClosePane,

		// Play
		PlayAll,

		// Git
		GitFetch,
		GitInit,
		GitPull,
		GitPush,
		GitSync,

		// Tags
		OpenAllTaggedItems,
	}
}
