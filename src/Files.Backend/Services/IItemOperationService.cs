using Files.Sdk.Storage.LocatableStorage;

namespace Files.Backend.Services
{
    /// <summary>
    /// Defines the core operations that can be performed for a storage item.
    /// </summary>
    public interface IItemOperationService
    {
        void Cut(ILocatableStorable item);
        void Copy(ILocatableStorable item);
        void Open(ILocatableStorable item);
        void Share(ILocatableStorable item);
        void Delete(ILocatableStorable item);
        void Rename(ILocatableStorable item);
        void Preview(ILocatableStorable item);
        void CopyPath(ILocatableStorable item);
    }
}
