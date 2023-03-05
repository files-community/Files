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

		// File System
		CopyItem,
		CutItem,
		DeleteItem,
		CreateFolder,
		CreateShortcut,
		CreateShortcutFromDialog,
		EmptyRecycleBin,
		RestoreRecycleBin,
		RestoreAllRecycleBin,

		// Selection
		MultiSelect,
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

		// Archives
		CompressIntoArchive
	}
}
