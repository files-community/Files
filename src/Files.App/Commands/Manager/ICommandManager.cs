using System;
using System.Collections.Generic;

namespace Files.App.Commands
{
	public interface ICommandManager : IEnumerable<IRichCommand>
	{
		IRichCommand this[CommandCodes code] { get; }
		IRichCommand this[HotKey customHotKey] { get; }

		IRichCommand None { get; }

		IRichCommand OpenHelp { get; }
		IRichCommand ToggleFullScreen { get; }

		IRichCommand ToggleShowHiddenItems { get; }
		IRichCommand ToggleShowFileExtensions { get; }
		IRichCommand TogglePreviewPane { get; }

		IRichCommand CopyItem { get; }
		IRichCommand CopyPath { get; }
		IRichCommand CutItem { get; }
		IRichCommand PasteItem { get; }
		IRichCommand PasteItemToSelection { get; }
		IRichCommand DeleteItem { get; }
		IRichCommand SelectAll { get; }
		IRichCommand InvertSelection { get; }
		IRichCommand ClearSelection { get; }
		IRichCommand CreateFolder { get; }
		IRichCommand CreateShortcut { get; }
		IRichCommand CreateShortcutFromDialog { get; }
		IRichCommand EmptyRecycleBin { get; }
		IRichCommand RestoreRecycleBin { get; }
		IRichCommand RestoreAllRecycleBin { get; }
		IRichCommand OpenItem { get; }
		IRichCommand OpenItemWithApplicationPicker { get; }
		IRichCommand OpenParentFolder { get; }

		IRichCommand PinToStart { get; }
		IRichCommand UnpinFromStart { get; }
		IRichCommand PinItemToFavorites { get; }
		IRichCommand UnpinItemFromFavorites { get; }

		IRichCommand SetAsWallpaperBackground { get; }
		IRichCommand SetAsSlideshowBackground { get; }
		IRichCommand SetAsLockscreenBackground { get; }

		IRichCommand RunAsAdmin { get; }
		IRichCommand RunAsAnotherUser { get; }

		IRichCommand LaunchQuickLook { get; }

		IRichCommand CompressIntoArchive { get; }
		IRichCommand CompressIntoSevenZip { get; }
		IRichCommand CompressIntoZip { get; }
		IRichCommand DecompressArchive { get; }
		IRichCommand DecompressArchiveHere { get; }
		IRichCommand DecompressArchiveToChildFolder { get; }

		IRichCommand RotateLeft { get; }
		IRichCommand RotateRight { get; }

		IRichCommand OpenTerminal { get; }
		IRichCommand OpenTerminalAsAdmin { get; }

		IRichCommand LayoutDecreaseSize { get; }
		IRichCommand LayoutIncreaseSize { get; }
		IRichCommand LayoutDetails { get; }
		IRichCommand LayoutTiles { get; }
		IRichCommand LayoutGridSmall { get; }
		IRichCommand LayoutGridMedium { get; }
		IRichCommand LayoutGridLarge { get; }
		IRichCommand LayoutColumns { get; }
		IRichCommand LayoutAdaptive { get; }

		IRichCommand SortByName { get; }
		IRichCommand SortByDateModified { get; }
		IRichCommand SortByDateCreated { get; }
		IRichCommand SortBySize { get; }
		IRichCommand SortByType { get; }
		IRichCommand SortBySyncStatus { get; }
		IRichCommand SortByTag { get; }
		IRichCommand SortByOriginalFolder { get; }
		IRichCommand SortByDateDeleted { get; }
		IRichCommand SortAscending { get; }
		IRichCommand SortDescending { get; }
		IRichCommand ToggleSortDirection { get; }
		IRichCommand ToggleSortDirectoriesAlongsideFiles { get; }

		IRichCommand GroupByNone { get; }
		IRichCommand GroupByName { get; }
		IRichCommand GroupByDateModified { get; }
		IRichCommand GroupByDateCreated { get; }
		IRichCommand GroupBySize { get; }
		IRichCommand GroupByType { get; }
		IRichCommand GroupBySyncStatus { get; }
		IRichCommand GroupByTag { get; }
		IRichCommand GroupByOriginalFolder { get; }
		IRichCommand GroupByDateDeleted { get; }
		IRichCommand GroupByFolderPath { get; }
		IRichCommand GroupAscending { get; }
		IRichCommand GroupDescending { get; }
		IRichCommand ToggleGroupDirection { get; }

		IRichCommand NewTab { get; }
		IRichCommand DuplicateCurrentTab { get; }
		IRichCommand DuplicateSelectedTab { get; }
		IRichCommand CloseTabsToTheLeftCurrent { get; }
		IRichCommand CloseTabsToTheLeftSelected { get; }
		IRichCommand CloseTabsToTheRightCurrent { get; }
		IRichCommand CloseTabsToTheRightSelected { get; }
		IRichCommand CloseOtherTabsCurrent { get; }
		IRichCommand CloseOtherTabsSelected { get; }

		IRichCommand InstallFont { get; }
	}
}
