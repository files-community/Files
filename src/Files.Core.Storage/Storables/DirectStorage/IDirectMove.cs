// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
        Task<INestedStorable> MoveFromAsync(INestedStorable itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default);
    }
}
