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
		void ShowProperties(RoutedEventArgs e);

		void OpenFileLocation(RoutedEventArgs e);

		void OpenDirectoryInNewTab(RoutedEventArgs e);

		void OpenDirectoryInNewPane(RoutedEventArgs e);

		void OpenInNewWindowItem(RoutedEventArgs e);

		void CreateNewFile(ShellNewEntry e);

		void ItemPointerPressed(PointerRoutedEventArgs e);

		void PointerWheelChanged(PointerRoutedEventArgs e);

		Task DragOver(DragEventArgs e);

		Task Drop(DragEventArgs e);

		void SearchUnindexedItems(RoutedEventArgs e);

		Task CreateFolderWithSelection(RoutedEventArgs e);

		Task PlayAll();

		void FormatDrive(ListedItem? obj);
	}
}
