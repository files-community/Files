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
		CopyItem,
		CutItem,
		DeleteItem,
		EmptyRecycleBin,
		RestoreRecycleBin,
		RestoreAllRecycleBin,

		// Favorites
		PinItemToFavorites,
		UnpinItemFromFavorites,
	}
}
