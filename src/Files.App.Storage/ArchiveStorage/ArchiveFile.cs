// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Storage
{
	/// <inheritdoc cref="IStorable"/>
	public class ArchiveFile : ArchiveStorable, IModifiableFile, ILocatableFile, INestedFile
	{
		public ArchiveFile(string path, string name, IFolder? parent)
			: base(path, name, parent)
		{
		}

		/// <inheritdoc/>
		public async Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}
	}
}
