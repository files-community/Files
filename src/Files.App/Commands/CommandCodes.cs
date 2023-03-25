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
		Find,

		// Show
		ToggleShowHiddenItems,
		ToggleShowFileExtensions,
		TogglePreviewPane,

		// File System
		CopyItem,
		CopyPath,
		CutItem,
		PasteItem,
		PasteItemToSelection,
		DeleteItem,
		CreateFolder,
		CreateShortcut,
		CreateShortcutFromDialog,
		EmptyRecycleBin,
		RestoreRecycleBin,
		RestoreAllRecycleBin,
		OpenItem,
		OpenItemWithApplicationPicker,
		OpenParentFolder,

		// Selection
		SelectAll,
		InvertSelection,
		ClearSelection,

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

		// QuickLook
		LaunchQuickLook,

		// Archives
		CompressIntoArchive,
		CompressIntoSevenZip,
		CompressIntoZip,
		DecompressArchive,
		DecompressArchiveHere,
		DecompressArchiveToChildFolder,

		// Image Edition
		RotateLeft,
		RotateRight,

		// Open
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
		DuplicateCurrentTab,
		DuplicateSelectedTab,
		CloseTabsToTheLeftCurrent,
		CloseTabsToTheLeftSelected,
		CloseTabsToTheRightCurrent,
		CloseTabsToTheRightSelected,
		CloseOtherTabsCurrent,
		CloseOtherTabsSelected,
	}
}
