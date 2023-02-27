namespace Files.App.Commands
{
	public enum CommandCodes
	{
		None,

		// global
		OpenHelp,
		OpenTerminal,
		OpenTerminalAsAdmin,
		ToggleFullScreen,

		// show
		ToggleShowHiddenItems,
		ToggleShowFileExtensions,

		// file system
		EmptyRecycleBin,
		RestoreRecycleBin,
		RestoreAllRecycleBin,
	}
}
