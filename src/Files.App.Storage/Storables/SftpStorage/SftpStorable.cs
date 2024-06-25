// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Renci.SshNet;

namespace Files.App.Storage.SftpStorage
{
	public abstract class SftpStorable : ILocatableStorable, INestedStorable
	{
		/// <inheritdoc/>
		public virtual string Path { get; protected set; }

		/// <inheritdoc/>
		public virtual string Name { get; protected set; }

		/// <inheritdoc/>
		public virtual string Id { get; }

		/// <summary>
		/// Gets the parent folder of the storable, if any.
		/// </summary>
		protected virtual IFolder? Parent { get; }

		protected internal SftpStorable(string path, string name, IFolder? parent)
		{
			Path = SftpHelpers.GetSftpPath(path);
			Name = name;
			Id = Path;
			Parent = parent;
		}

		/// <inheritdoc/>
		public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Parent);
		}

		protected SftpClient GetSftpClient()
			=> SftpHelpers.GetSftpClient(Path);
	}
}
