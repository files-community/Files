// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Commands
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
		EditPath,
		Redo,
		Undo,

		// Show
		ToggleShowHiddenItems,
		ToggleShowFileExtensions,
		TogglePreviewPane,
		ToggleSidebar,

		// File System
		CopyItem,
		CopyPath,
		CutItem,
		PasteItem,
		PasteItemToSelection,
		DeleteItem,
		DeleteItemPermanently,
		CreateFolder,
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

		// Run
		RunAsAdmin,
		RunAsAnotherUser,
		RunWithPowershell,

		// QuickLook
		LaunchQuickLook,

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
		OpenSettings,
		OpenTerminal,
		OpenTerminalAsAdmin,

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
		GroupAscending,
		GroupDescending,
		ToggleGroupDirection,

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
		ReopenClosedTab,
		PreviousTab,
		NextTab,
		CloseSelectedTab,
		OpenNewPane,
		ClosePane,

		// Play
		PlayAll,
	}
}
