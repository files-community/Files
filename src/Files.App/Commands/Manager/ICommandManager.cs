using System;
using System.Collections.Generic;

namespace Files.App.Commands
{
	public interface ICommandManager : IEnumerable<IRichCommand>
	{
		event EventHandler<HotKeyChangedEventArgs>? HotKeyChanged;

		IRichCommand this[CommandCodes code] { get; }
		IRichCommand this[HotKey customHotKey] { get; }

		IRichCommand None { get; }

		IRichCommand OpenHelp { get; }
		IRichCommand ToggleFullScreen { get; }

		IRichCommand ToggleShowHiddenItems { get; }
		IRichCommand ToggleShowFileExtensions { get; }
		IRichCommand TogglePreviewPane { get; }

		IRichCommand CopyItem { get; }
		IRichCommand CutItem { get; }
		IRichCommand DeleteItem { get; }
		IRichCommand MultiSelect { get; }
		IRichCommand SelectAll { get; }
		IRichCommand InvertSelection { get; }
		IRichCommand ClearSelection { get; }
		IRichCommand CreateFolder { get; }
		IRichCommand CreateShortcut { get; }
		IRichCommand CreateShortcutFromDialog { get; }
		IRichCommand EmptyRecycleBin { get; }
		IRichCommand RestoreRecycleBin { get; }
		IRichCommand RestoreAllRecycleBin { get; }

		IRichCommand PinToStart { get; }
		IRichCommand UnpinFromStart { get; }
		IRichCommand PinItemToFavorites { get; }
		IRichCommand UnpinItemFromFavorites { get; }

		IRichCommand SetAsWallpaperBackground { get; }
		IRichCommand SetAsSlideshowBackground { get; }
		IRichCommand SetAsLockscreenBackground { get; }

		IRichCommand RunAsAdmin { get; }
		IRichCommand RunAsAnotherUser { get; }

		IRichCommand CompressIntoArchive { get; }
		IRichCommand CompressIntoSevenZip { get; }
		IRichCommand CompressIntoZip { get; }

		IRichCommand RotateLeft { get; }
		IRichCommand RotateRight { get; }
    
    IRichCommand OpenAllTaggedItems { get; }
	}
}
