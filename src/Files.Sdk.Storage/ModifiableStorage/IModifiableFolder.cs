using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.ModifiableStorage
{
	/// <summary>
	/// Represents a folder that can be modified.
	/// </summary>
	public interface IModifiableFolder : IFolder, IModifiableStorable
	{
		/// <summary>
		/// Deletes the provided storable item from this folder.
		/// </summary>
		Task DeleteAsync(IStorable item, bool permanently = false, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a copy of the provided storable item in this folder.
		/// </summary>
		Task<IStorable> CreateCopyOfAsync(IStorable itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default);

		/// <summary>
		/// Moves a storable item out of the provided folder, and into this folder. Returns the new item that resides in this folder.
		/// </summary>
		Task<IStorable> MoveFromAsync(IStorable itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new file with the desired name inside this folder.
		/// </summary>
		Task<IFile> CreateFileAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new folder with the desired name inside this folder.
		/// </summary>
		Task<IFolder> CreateFolderAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default);
	}
}
