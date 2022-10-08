using Files.Sdk.Storage.LocatableStorage;
using Files.Shared;
using System.Reflection;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
    public interface IItemCommandService
    {
        void RenameItem(ILocatableStorable item);

        void CreateShortcut();

        void SetAsLockscreenBackgroundItem();

        void SetAsDesktopBackgroundItem();

        void SetAsSlideshowItem();

        void RunAsAdmin();

        void RunAsAnotherUser();

        void SidebarPinItem();

        void SidebarUnpinItem();

        void UnpinDirectoryFromFavorites();

        void OpenItem();

        void EmptyRecycleBin();

        void RestoreRecycleBin();

        void RestoreSelectionRecycleBin();

        void QuickLook();

        void CopyItem();

        void CutItem();

        void RestoreItem();

        void DeleteItem();

        void ShowFolderProperties();

        void ShowProperties();

        void OpenFileLocation();

        void OpenParentFolder();

        void OpenItemWithApplicationPicker();

        void OpenDirectoryInNewTab();

        void OpenDirectoryInNewPane();

        void OpenInNewWindowItem();

        void CreateNewFolder();

        void CreateNewFile(ShellNewEntry e);

        void PasteItemsFromClipboard();

        void CopyPathOfSelectedItem();

        void OpenDirectoryInDefaultTerminal();

        void ShareItem();

        void PinDirectoryToFavorites();

        void ItemPointerPressed(Pointer);

        void UnpinItemFromStart();

        void PinItemToStart();

        void PointerWheelChanged(Pointer);

        void GridViewSizeDecrease(KeyboardAcceleratorInvokedEventArgs e);

        void GridViewSizeIncrease(KeyboardAcceleratorInvokedEventArgs e);

        Task DragOver(DragEventArgs e);

        Task Drop(DragEventArgs e);

        void RefreshItems();

        void SearchUnindexedItems();

        Task CreateFolderWithSelection();

        Task DecompressArchive();

        Task DecompressArchiveHere();

        Task DecompressArchiveToChildFolder();

        Task InstallInfDriver();

        Task RotateImageLeft();

        Task RotateImageRight();

        Task InstallFont();
    }
}
