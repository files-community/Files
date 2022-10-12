using Files.Sdk.Storage.LocatableStorage;
using System.Collections.Generic;

namespace Files.Backend.Services
{
    /// <summary>
    /// Defines the operations that can be performed with the operating system
    /// pre-deletion construct such as Recycle Bin on Windows.
    /// </summary>
    public interface ITrashService
    {
        void Empty();
        void Remove(ILocatableStorable item); 
        void RestoreAll();
        void Restore(ILocatableStorable item);
        IList<ILocatableStorable> GetItems();
    }
}
