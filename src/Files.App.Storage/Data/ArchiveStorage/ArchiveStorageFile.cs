// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Storage
{
	/// <inheritdoc cref="IStorable"/>
	public class ArchiveStorageFile : ArchiveStorable, IModifiableFile, ILocatableFile, INestedFile, IDirectCopy, IDirectMove
	{
		public ArchiveStorageFile(string path, string name, IFolder? parent)
			: base(path, name, parent)
		{
		}

		/// <inheritdoc/>
		public Task<INestedStorable> CopyAsync(INestedStorable itemToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public Task DeleteAsync(INestedStorable item, bool permanently = false, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public Task<INestedStorable> MoveAsync(INestedStorable itemToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public async Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}
	}
}
