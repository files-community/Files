using Files.Sdk.Storage.LocatableStorage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
    public interface ITrashService
    {
        void Empty();
        void Restore();
        Task<IList<ILocatableStorable>> GetItemsAsync();
    }
}
