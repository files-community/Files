// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.LocatableStorage;
using Files.Core.Storage.ModifiableStorage;
using Files.Core.Storage.NestedStorage;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	public sealed class FtpStorageFile : FtpStorable, IModifiableFile, ILocatableFile, INestedFile
	{
		public FtpStorageFile(string path, string name, IFolder? parent)
			: base(path, name, parent)
		{
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
