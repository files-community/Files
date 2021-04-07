using Files.DataModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Files.Interacts
{
    public interface IBaseLayoutCommandImplementationModel : IDisposable
    {
        void CopyItem(RoutedEventArgs e);

        void CopyPathOfSelectedItem(RoutedEventArgs e);

        void CreateNewFile(ShellNewEntry e);

        void CreateNewFolder(RoutedEventArgs e);

        void CreateShortcut(RoutedEventArgs e);

        void CutItem(RoutedEventArgs e);

        void DeleteItem(RoutedEventArgs e);

        void DragEnter(DragEventArgs e);

        void Drop(DragEventArgs e);

        void EmptyRecycleBin(RoutedEventArgs e);

        void GridViewSizeDecrease(KeyboardAcceleratorInvokedEventArgs e);

        void GridViewSizeIncrease(KeyboardAcceleratorInvokedEventArgs e);

        void ItemPointerPressed(PointerRoutedEventArgs e);

        void OpenDirectoryInDefaultTerminal(RoutedEventArgs e);

        void OpenDirectoryInNewPane(RoutedEventArgs e);

        void OpenDirectoryInNewTab(RoutedEventArgs e);

        void OpenFileLocation(RoutedEventArgs e);

        void OpenInNewWindowItem(RoutedEventArgs e);

        void OpenItem(RoutedEventArgs e);

        void OpenItemWithApplicationPicker(RoutedEventArgs e);

        void PasteItemsFromClipboard(RoutedEventArgs e);

        void PinDirectoryToSidebar(RoutedEventArgs e);

        void PinItemToStart(RoutedEventArgs e);

        void PointerWheelChanged(PointerRoutedEventArgs e);

        void QuickLook(RoutedEventArgs e);

        void RefreshItems(RoutedEventArgs e);

        void RenameItem(RoutedEventArgs e);

        void RestoreItem(RoutedEventArgs e);

        void RunAsAdmin(RoutedEventArgs e);

        void RunAsAnotherUser(RoutedEventArgs e);

        void SearchUnindexedItems(RoutedEventArgs e);

        void SetAsDesktopBackgroundItem(RoutedEventArgs e);

        void SetAsLockscreenBackgroundItem(RoutedEventArgs e);

        void ShareItem(RoutedEventArgs e);

        void ShowFolderProperties(RoutedEventArgs e);

        void ShowProperties(RoutedEventArgs e);

        void SidebarPinItem(RoutedEventArgs e);

        void SidebarUnpinItem(RoutedEventArgs e);

        void UnpinDirectoryFromSidebar(RoutedEventArgs e);

        void UnpinItemFromStart(RoutedEventArgs e);
    }
}