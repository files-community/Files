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

		// Start
		PinToStart,
		UnpinFromStart,
	}
}
