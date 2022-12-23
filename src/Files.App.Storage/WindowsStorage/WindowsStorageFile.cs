using Files.App.Storage.Extensions;
using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.ModifiableStorage;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Storage.WindowsStorage
{
	/// <inheritdoc cref="IFile"/>
	public sealed class WindowsStorageFile : WindowsStorable<StorageFile>, ILocatableFile, IModifiableFile
	{
		public WindowsStorageFile(StorageFile storage) : base(storage) {}

		/// <inheritdoc/>
		public override async Task<ILocatableFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var parent = await Storage.GetParentAsync().AsTask(cancellationToken);
			return new WindowsStorageFolder(parent);
		}

		/// <inheritdoc/>
		public async Task<Stream> OpenStreamAsync(FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			var fileAccessMode = access.ToFileAccessMode();
			var storageOpenOptions = share.ToStorageOpenOptions();

			var winrtStream = await Storage.OpenAsync(fileAccessMode, storageOpenOptions).AsTask(cancellationToken);
			return winrtStream.AsStream();
		}
	}
}
