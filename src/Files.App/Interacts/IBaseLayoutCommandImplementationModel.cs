using Files.App.Filesystem;
using Files.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;

namespace Files.App.Interacts
{
	public interface IBaseLayoutCommandImplementationModel : IDisposable
	{
		void RenameItem(RoutedEventArgs e);

		void CreateShortcut(RoutedEventArgs e);

		void CreateShortcutFromDialog(RoutedEventArgs e);

		void SetAsLockscreenBackgroundItem(RoutedEventArgs e);

		void SetAsDesktopBackgroundItem(RoutedEventArgs e);

		void SetAsSlideshowItem(RoutedEventArgs e);

		void RunAsAdmin(RoutedEventArgs e);

		void RunAsAnotherUser(RoutedEventArgs e);

		void OpenItem(RoutedEventArgs e);

		void RestoreRecycleBin(RoutedEventArgs e);

		void RestoreSelectionRecycleBin(RoutedEventArgs e);

		void QuickLook(RoutedEventArgs e);

		void CopyItem(RoutedEventArgs e);

		void CutItem(RoutedEventArgs e);

		void RestoreItem(RoutedEventArgs e);

		void DeleteItem(RoutedEventArgs e);

		void ShowFolderProperties(RoutedEventArgs e);

		void ShowProperties(RoutedEventArgs e);

		void OpenFileLocation(RoutedEventArgs e);

		void OpenParentFolder(RoutedEventArgs e);

		void OpenItemWithApplicationPicker(RoutedEventArgs e);

		void OpenDirectoryInNewTab(RoutedEventArgs e);

		void OpenDirectoryInNewPane(RoutedEventArgs e);

		void OpenInNewWindowItem(RoutedEventArgs e);

		void CreateNewFolder(RoutedEventArgs e);

		void CreateNewFile(ShellNewEntry e);

		void PasteItemsFromClipboard(RoutedEventArgs e);

		void CopyPathOfSelectedItem(RoutedEventArgs e);

		void ShareItem(RoutedEventArgs e);

		void PinDirectoryToFavorites(RoutedEventArgs e);

		void ItemPointerPressed(PointerRoutedEventArgs e);

		void UnpinItemFromStart(RoutedEventArgs e);

		void PinItemToStart(RoutedEventArgs e);

		void PointerWheelChanged(PointerRoutedEventArgs e);

		void GridViewSizeDecrease(KeyboardAcceleratorInvokedEventArgs e);

		void GridViewSizeIncrease(KeyboardAcceleratorInvokedEventArgs e);

		Task DragOver(DragEventArgs e);

		Task Drop(DragEventArgs e);

		void RefreshItems(RoutedEventArgs e);

		void SearchUnindexedItems(RoutedEventArgs e);

		Task CreateFolderWithSelection(RoutedEventArgs e);

		Task CompressIntoArchive();

		Task CompressIntoZip();

		Task CompressIntoSevenZip();

		Task DecompressArchive();

		Task DecompressArchiveHere();

		Task DecompressArchiveToChildFolder();

		Task InstallInfDriver();

		Task RotateImageLeft();

		Task RotateImageRight();

		Task InstallFont();

		Task PlayAll();

		void FormatDrive(ListedItem? obj);
	}
}
