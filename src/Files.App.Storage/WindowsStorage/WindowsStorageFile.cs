// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.ExtendableStorage;
using Files.Core.Storage.LocatableStorage;
using Files.Core.Storage.ModifiableStorage;
using Files.Core.Storage.NestedStorage;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Storage.WindowsStorage
{
	/// <inheritdoc cref="IFile"/>
	public sealed class WindowsStorageFile : WindowsStorable<StorageFile>, ILocatableFile, IModifiableFile, IFileExtended, INestedFile
	{
		public WindowsStorageFile(StorageFile storage)
			: base(storage)
		{
		}

		/// <inheritdoc/>
		public Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
		{
			return OpenStreamAsync(access, FileShare.None, cancellationToken);
		}

		/// <inheritdoc/>
		public async Task<Stream> OpenStreamAsync(FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			var fileAccessMode = GetFileAccessMode(access);
			var storageOpenOptions = GetStorageOpenOptions(share);

			var winrtStreamTask = storage.OpenAsync(fileAccessMode, storageOpenOptions).AsTask(cancellationToken);
			var winrtStream = await winrtStreamTask;

			return winrtStream.AsStream();
		}

		/// <inheritdoc/>
		public override async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var parentFolderTask = storage.GetParentAsync().AsTask(cancellationToken);
			var parentFolder = await parentFolderTask;

			return new WindowsStorageFolder(parentFolder);
		}

		private static FileAccessMode GetFileAccessMode(FileAccess access)
		{
			return access switch
			{
				FileAccess.Read => FileAccessMode.Read,
				FileAccess.Write => FileAccessMode.ReadWrite,
				FileAccess.ReadWrite => FileAccessMode.ReadWrite,
				_ => throw new ArgumentOutOfRangeException(nameof(access))
			};
		}

		private static StorageOpenOptions GetStorageOpenOptions(FileShare share)
		{
			return share switch
			{
				FileShare.Read => StorageOpenOptions.AllowOnlyReaders,
				FileShare.Write => StorageOpenOptions.AllowReadersAndWriters,
				FileShare.ReadWrite => StorageOpenOptions.AllowReadersAndWriters,
				FileShare.Inheritable => StorageOpenOptions.None,
				FileShare.Delete => StorageOpenOptions.None,
				FileShare.None => StorageOpenOptions.None,
				_ => throw new ArgumentOutOfRangeException(nameof(share))
			};
		}
	}
}
