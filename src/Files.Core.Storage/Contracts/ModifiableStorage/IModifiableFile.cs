// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage
{
	/// <summary>
	/// Represents a file that can be modified.
	/// </summary>
	public interface IModifiableFile : IFile, IModifiableStorable
	{
		/// <summary>
		/// Deletes the provided storable item from this folder.
		/// </summary>
		Task DeleteAsync(INestedStorable item, bool permanently = default, CancellationToken cancellationToken = default);
	}
}
