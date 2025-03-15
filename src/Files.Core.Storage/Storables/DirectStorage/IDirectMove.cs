// Copyright (c) Files Community
// Licensed under the MIT License.

using OwlCore.Storage;

namespace Files.Core.Storage.Storables
{
	/// <summary>
	/// Provides direct move operation of storage objects.
	/// </summary>
	public interface IDirectMove : IModifiableFolder
    {
        /// <summary>
        /// Moves a storable item out of the provided folder, and into this folder. Returns the new item that resides in this folder.
        /// </summary>
        Task<IStorableChild> MoveFromAsync(IStorableChild itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default);
    }
}
