using Files.Sdk.Storage.LocatableStorage;

namespace Files.Backend.Services
{
    /// <summary>
    /// Defines the operations that can be performed for a compressed archive.
    /// </summary>
    public interface IArchiveOperationService
    {
        void DecompressArchive(ILocatableFile archive);
    }
}
