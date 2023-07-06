// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using Files.Shared.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.NativeStorage
{
	/// <inheritdoc cref="IStorable"/>
	public abstract class NativeStorable : ILocatableStorable
	{
		private string? _computedId;

		/// <inheritdoc/>
		public string Path { get; protected set; }

		/// <inheritdoc/>
		public string Name { get; protected set; }

		/// <inheritdoc/>
		public virtual string Id => _computedId ??= ChecksumHelpers.CalculateChecksumForPath(Path);

		protected NativeStorable(string path)
		{
			Path = path;
			Name = System.IO.Path.GetFileName(path);
		}

		/// <inheritdoc/>
		public virtual Task<ILocatableFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var parentPath = System.IO.Path.GetDirectoryName(Path);
			if (string.IsNullOrEmpty(parentPath))
				return Task.FromResult<ILocatableFolder?>(null);

			return Task.FromResult<ILocatableFolder?>(new NativeFolder(parentPath));
		}
	}
}
