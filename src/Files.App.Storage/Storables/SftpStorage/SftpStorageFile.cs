// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Storage.SftpStorage
{
	public sealed class SftpStorageFile : SftpStorable, IModifiableFile, ILocatableFile, INestedFile
	{
		public SftpStorageFile(string path, string name, IFolder? parent)
			: base(path, name, parent)
		{
		}

		/// <inheritdoc/>
		public async Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
		{
			using var sftpClient = GetSftpClient();
			await sftpClient.EnsureConnectedAsync(cancellationToken);

			if (access.HasFlag(FileAccess.Write))
				return await sftpClient.OpenAsync(Path, FileMode.Open, FileAccess.Write, cancellationToken);
			else if (access.HasFlag(FileAccess.Read))
				return await sftpClient.OpenAsync(Path, FileMode.Open, FileAccess.Read, cancellationToken);
			else
				throw new ArgumentException($"Invalid {nameof(access)} flag.");
		}
	}
}
