using Files.Sdk.Storage.LocatableStorage;

namespace Files.Backend.Services
{
    /// <summary>
    /// Defines the operations that can be performed within a folder.
    /// </summary>
    public interface IFolderOperationService
    {
        void Paste(ILocatableFolder location);
        void OpenInTerminal(ILocatableFolder location);
        void CreateFile(ILocatableFolder location, string name);
        void CreateFolder(ILocatableFolder location, string name);
        void CreateShortcut(ILocatableFolder location, ILocatableStorable item);
    }
}
