// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage
{
	/// <summary>
	/// Provides direct move operation of storage objects.
	/// </summary>
	public interface IDirectMove : IModifiableStorable
	{
		/// <summary>
		/// Moves a storable item out of the provided folder, and into this folder. Returns the new item that resides in this folder.
		/// </summary>
		Task<INestedStorable> MoveAsync(INestedStorable itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default);
	}
}
