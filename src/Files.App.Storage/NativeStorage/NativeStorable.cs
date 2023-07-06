// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.NestedStorage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.NativeStorage
{
	/// <inheritdoc cref="IStorable"/>
	public abstract class NativeStorable<TStorage> : ILocatableStorable, INestedStorable
		where TStorage : FileSystemInfo
	{
		protected readonly TStorage storage;

		/// <inheritdoc/>
		public virtual string Path { get; protected set; }

		/// <inheritdoc/>
		public virtual string Name { get; protected set; }

		/// <inheritdoc/>
		public virtual string Id { get; }

		protected NativeStorable(TStorage storage)
		{
			this.storage = storage;
			Path = storage.FullName;
			Name = storage.Name;
			Id = storage.FullName;
		}

		/// <inheritdoc/>
		public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var parent = Directory.GetParent(Path);
			if (parent is null)
				return Task.FromResult<IFolder?>(null);

			return Task.FromResult<IFolder?>(new NativeFolder(parent));
		}

		/// <summary>
		/// Formats a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to format.</param>
		/// <returns>A formatted path.</returns>
		protected static string FormatPath(string path)
		{
			path = path.Replace("file:///", string.Empty);

			if ('/' != System.IO.Path.DirectorySeparatorChar)
				return path.Replace('/', System.IO.Path.DirectorySeparatorChar);

			if ('\\' != System.IO.Path.DirectorySeparatorChar)
				return path.Replace('\\', System.IO.Path.DirectorySeparatorChar);

			return path;
		}
	}
}
