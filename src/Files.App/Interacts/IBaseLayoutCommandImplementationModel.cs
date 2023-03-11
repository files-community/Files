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

		void OpenItem(RoutedEventArgs e);

		void ShowProperties(RoutedEventArgs e);

		void OpenFileLocation(RoutedEventArgs e);

		void OpenParentFolder(RoutedEventArgs e);

		void OpenItemWithApplicationPicker(RoutedEventArgs e);

		void OpenDirectoryInNewTab(RoutedEventArgs e);

		void OpenDirectoryInNewPane(RoutedEventArgs e);

		void OpenInNewWindowItem(RoutedEventArgs e);

		void CreateNewFile(ShellNewEntry e);

		void PasteItemsFromClipboard(RoutedEventArgs e);

		void CopyPathOfSelectedItem(RoutedEventArgs e);

		void ShareItem(RoutedEventArgs e);

		void ItemPointerPressed(PointerRoutedEventArgs e);

		void PointerWheelChanged(PointerRoutedEventArgs e);

		void GridViewSizeDecrease(KeyboardAcceleratorInvokedEventArgs e);

		void GridViewSizeIncrease(KeyboardAcceleratorInvokedEventArgs e);

		Task DragOver(DragEventArgs e);

		Task Drop(DragEventArgs e);

		void RefreshItems(RoutedEventArgs e);

		void SearchUnindexedItems(RoutedEventArgs e);

		Task CreateFolderWithSelection(RoutedEventArgs e);

		Task DecompressArchiveToChildFolder();

		Task InstallInfDriver();

		Task InstallFont();

		Task PlayAll();

		void FormatDrive(ListedItem? obj);
	}
}
