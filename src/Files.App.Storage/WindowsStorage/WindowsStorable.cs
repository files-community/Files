// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.LocatableStorage;
using Files.Core.Storage.NestedStorage;
using Files.Shared.Helpers;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Storage.WindowsStorage
{
	/// <inheritdoc cref="IStorable"/>
	public abstract class WindowsStorable<TStorage> : ILocatableStorable, INestedStorable
		where TStorage : class, IStorageItem
	{
		private string? _computedId;
		internal readonly TStorage storage;

		/// <inheritdoc/>
		public string Path { get; protected internal set; }

		/// <inheritdoc/>
		public string Name { get; protected internal set; }

		/// <inheritdoc/>
		public virtual string Id => _computedId ??= ChecksumHelpers.CalculateChecksumForPath(Path);

		protected internal WindowsStorable(TStorage storage)
		{
			this.storage = storage;
			Path = storage.Path;
			Name = storage.Name;
		}

		/// <inheritdoc/>
		public abstract Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default);
	}
}
