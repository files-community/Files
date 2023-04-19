using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

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
	}
}
