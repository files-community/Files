using Files.Sdk.Storage.LocatableStorage;

namespace Files.Backend.Services
{
    public interface IItemOperationService
    {
        void Rename(ILocatableStorable item);
        void Copy(ILocatableStorable item);
        void Cut(ILocatableStorable item);
        void Delete(ILocatableStorable item);
        void CopyPath(ILocatableStorable item);


        //todo: add to service

        //void CreateShortcut(ILocatableStorable item);

        //void SetAsLockscreenBackgroundItem(ILocatableStorable item);
        //void SetAsDesktopBackgroundItem(ILocatableStorable item);
        //void SetAsSlideshowItem(ILocatableStorable item);


        //void Open();
        //void RunAsAdmin();
        //void RunAsAnotherUser();


        //sidebar view model
        //void PinItem(ILocatableStorable item);
        //void UnpinItem(ILocatableStorable item);

        //void QuickLook();

        //void RestoreItem();

        //void ShowProperties();

        //void CreateNewFolder();

        //void CreateNewFile(ShellNewEntry e);

        //void PasteItemsFromClipboard();


        //void OpenDirectoryInTerminal();

        //void ShareItem();


        //void UnpinFromStartScreen();
        //void PinToStartScreen();

        //Task DecompressArchive();
    }
}
