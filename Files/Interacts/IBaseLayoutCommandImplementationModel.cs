using System;
using Windows.UI.Xaml;

namespace Files.Interacts
{
    public interface IBaseLayoutCommandImplementationModel : IDisposable
    {
        void RenameItem(RoutedEventArgs e);

        void CreateShortcut(RoutedEventArgs e);

        void SetAsLockscreenBackgroundItem(RoutedEventArgs e);

        void SetAsDesktopBackgroundItem(RoutedEventArgs e);

        void RunAsAdmin(RoutedEventArgs e);

        void RunAsAnotherUser(RoutedEventArgs e);

        void SidebarPinItem(RoutedEventArgs e);

        void SidebarUnpinItem(RoutedEventArgs e);

        void UnpinDirectoryFromSidebar(RoutedEventArgs e);

        void OpenItem(RoutedEventArgs e);

        void EmptyRecycleBin(RoutedEventArgs e);

        void QuickLook(RoutedEventArgs e);

        void CopyItem(RoutedEventArgs e);

        void CutItem(RoutedEventArgs e);

        void RestoreItem(RoutedEventArgs e);

        void DeleteItem(RoutedEventArgs e);

        void ShowFolderProperties(RoutedEventArgs e);

        void ShowProperties(RoutedEventArgs e);

        void OpenFileLocation(RoutedEventArgs e);

        void OpenItemWithApplicationPicker(RoutedEventArgs e);

        void OpenDirectoryInNewTab(RoutedEventArgs e);

        void OpenDirectoryInNewPane(RoutedEventArgs e);

        void OpenInNewWindowItem(RoutedEventArgs e);
    }
}
