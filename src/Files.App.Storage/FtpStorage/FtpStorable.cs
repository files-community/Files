// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.LocatableStorage;
using Files.Core.Storage.NestedStorage;
using FluentFTP;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	public abstract class FtpStorable : ILocatableStorable, INestedStorable
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

		protected internal FtpStorable(string path, string name, IFolder? parent)
		{
			Path = FtpHelpers.GetFtpPath(path);
			Name = name;
			Id = Path;
			Parent = parent;
		}

		/// <inheritdoc/>
		public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IFolder?>(Parent);
		}

		protected AsyncFtpClient GetFtpClient()
		{
			return FtpHelpers.GetFtpClient(Path);
		}
	}
}
