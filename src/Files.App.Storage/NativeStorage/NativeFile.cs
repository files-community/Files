// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.ExtendableStorage;
using Files.Core.Storage.LocatableStorage;
using Files.Core.Storage.ModifiableStorage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.NativeStorage
{
	/// <inheritdoc cref="IFile"/>
    public sealed class NativeFile : NativeStorable, ILocatableFile, IModifiableFile, IFileExtended
	{
		public NativeFile(string path)
			: base(path)
		{
		}

		/// <inheritdoc/>
		public Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
		{
			return OpenStreamAsync(access, FileShare.None, cancellationToken);
		}

		/// <inheritdoc/>
		public Task<Stream> OpenStreamAsync(FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			var stream = File.Open(Path, FileMode.Open, access, share);
			return Task.FromResult<Stream>(stream);
		}
	}
}
