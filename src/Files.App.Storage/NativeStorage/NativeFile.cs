// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage;
using Files.Sdk.Storage.ExtendableStorage;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.ModifiableStorage;
using Files.Sdk.Storage.NestedStorage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.NativeStorage
{
	/// <inheritdoc cref="IFile"/>
	public class NativeFile : NativeStorable<FileInfo>, ILocatableFile, IModifiableFile, IFileExtended, INestedFile
	{
		public NativeFile(FileInfo fileInfo)
			: base(fileInfo)
		{
		}

		public NativeFile(string path)
			: this(new FileInfo(path))
		{
		}

		/// <inheritdoc/>
		public virtual Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
		{
			return OpenStreamAsync(access, FileShare.None, cancellationToken);
		}

		/// <inheritdoc/>
		public virtual Task<Stream> OpenStreamAsync(FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			var stream = File.Open(Path, FileMode.Open, access, share);
			return Task.FromResult<Stream>(stream);
		}
	}
}
