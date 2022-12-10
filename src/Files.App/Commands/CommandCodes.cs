namespace Files.App.Commands
{
	public enum CommandCodes
	{
		None,

		// global
		Help,
		FullScreen,

		// setting
		ShowHiddenItems,
		ShowFileExtensions,

		// layout
		LayoutDetails,
		LayoutTiles,
		LayoutGridSmall,
		LayoutGridMedium,
		LayoutGridLarge,
		LayoutColumns,
		LayoutAdaptive,

		// selection
		MultiSelect,
		SelectAll,
		InvertSelection,
		ClearSelection,

		// folder
		OpenFolderInNewTab,

		// item
		Rename,
		Properties,
	}
}
