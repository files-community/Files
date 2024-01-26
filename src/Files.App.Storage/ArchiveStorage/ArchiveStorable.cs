// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Storage
{
	/// <inheritdoc cref="IStorable"/>
	public abstract class ArchiveStorable : ILocatableStorable, INestedStorable
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

		protected internal ArchiveStorable(string path, string name, IFolder? parent)
		{
			Path = path;
			Name = name;
			Id = Path;
			Parent = parent;
		}

		/// <inheritdoc/>
		public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Parent);
		}
	}
}
