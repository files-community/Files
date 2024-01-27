// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Storage
{
	public sealed class FtpStorageFile : FtpStorable, IModifiableFile, ILocatableFile, INestedFile, IDirectCopy, IDirectMove
	{
		public FtpStorageFile(string path, string name, IFolder? parent)
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
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			if (access.HasFlag(FileAccess.Write))
				return await ftpClient.OpenWrite(Path, token: cancellationToken);
			else if (access.HasFlag(FileAccess.Read))
				return await ftpClient.OpenRead(Path, token: cancellationToken);
			else
				throw new ArgumentException($"Invalid {nameof(access)} flag.");
		}
	}
}
