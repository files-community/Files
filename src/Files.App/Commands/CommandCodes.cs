namespace Files.App.Commands
{
	public enum CommandCodes
	{
		None,

		// Global
		OpenHelp,
		ToggleFullScreen,

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

		// Run
		RunAsAdmin,
		RunAsAnotherUser,

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
		DuplicateTab,
	}
}
