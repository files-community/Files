using Files.Sdk.Storage.ModifiableStorage;
using Files.Sdk.Storage.NestedStorage;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.DirectStorage
{
	/// <summary>
	/// Provides direct copy operation of storage objects.
	/// </summary>
	public interface IDirectCopy : IModifiableFolder
	{
		/// <summary>
		/// Creates a copy of the provided storable item in this folder.
		/// </summary>
		Task<INestedStorable> CreateCopyOfAsync(INestedStorable itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default);
	}
}
