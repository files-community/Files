namespace Files.App.Keyboard
{
	public enum KeyboardActionCodes : ushort
	{
		None,

		// general
		Help,

		// selection
		ToggleMultiSelection,
		SelectAll,
		InvertSelection,
		ClearSelection,

		// layout
		ToggleLayoutDetails,
		ToggleLayoutTiles,
		ToggleLayoutGridSmall,
		ToggleLayoutGridMedium,
		ToggleLayoutGridLarge,
		ToggleLayoutColumns,
		ToggleLayoutAdaptive,

		// file
		ToggleShowHiddenItems,
		ToggleShowFileExtensions,
	}
}
