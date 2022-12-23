using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.ModifiableStorage;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	/// <inheritdoc cref="IFile"/>
	public sealed class FtpStorageFile : FtpStorable, IModifiableFile, ILocatableFile
	{
		public FtpStorageFile(string path, string name) : base(path, name) {}

		/// <inheritdoc/>
		public async Task<Stream> OpenStreamAsync(FileAccess access, FileShare share, CancellationToken cancellationToken = default)
		{
			using var ftpClient = await GetFtpClient(cancellationToken);

			if (access.HasFlag(FileAccess.Write))
				return await ftpClient.OpenWrite(Path, token: cancellationToken);

			if (access.HasFlag(FileAccess.Read))
				return await ftpClient.OpenRead(Path, token: cancellationToken);

			throw new ArgumentException($"Invalid {nameof(share)} flag.");
		}
	}
}
