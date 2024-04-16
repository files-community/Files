using Files.Core.Storage.ModifiableStorage;
using Files.Core.Storage.NestedStorage;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Core.Storage.DirectStorage
{
	/// <summary>
	/// Provides direct move operation of storage objects.
	/// </summary>
	public interface IDirectMove : IModifiableFolder
    {
        /// <summary>
        /// Moves a storable item out of the provided folder, and into this folder. Returns the new item that resides in this folder.
        /// </summary>
        Task<INestedStorable> MoveFromAsync(INestedStorable itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default);
    }
}
