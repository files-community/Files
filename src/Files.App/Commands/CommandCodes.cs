namespace Files.App.Commands
{
	public enum CommandCodes
	{
		None,

		// global
		OpenHelp,
		ToggleFullScreen,

		// show
		ToggleShowHiddenItems,
		ToggleShowFileExtensions,

		// file system
		CreateFolder,
		CreateShortcut,
		CreateShortcutFromDialog,
		EmptyRecycleBin,
		RestoreRecycleBin,
		RestoreAllRecycleBin,

		// selection
		MultiSelect,
		SelectAll,
		InvertSelection,
		ClearSelection,

		// start
		PinToStart,
		UnpinFromStart,

		// favorites
		PinItemToFavorites,
		UnpinItemFromFavorites,
	}
}
