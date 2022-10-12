using Files.Sdk.Storage.LocatableStorage;

namespace Files.Backend.Services
{
    /// <summary>
    /// Defines the operations that can be performed to personalize
    /// operating system surfaces with a storage item.
    /// </summary>
    public interface IPersonalizationService
    {
        void SetAsLockscreenBackground(ILocatableStorable item);
        void SetAsDesktopBackground(ILocatableStorable item);
        void SetAsSlideshow(ILocatableStorable item);
        void AddToStartScreen(ILocatableStorable item);
        void RemoveFromStartScreen(ILocatableStorable item);
    }
}
