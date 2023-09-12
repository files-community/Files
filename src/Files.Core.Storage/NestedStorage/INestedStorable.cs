// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Threading;
using System.Threading.Tasks;

namespace Files.Core.Storage.NestedStorage
{
	/// <summary>
	/// Represents a storable resource that resides within a traversable folder structure.
	/// </summary>
	public interface INestedStorable : IStorable
	{
		/// <summary>
		/// Gets the containing folder for this item, if any.
		/// </summary>
		Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default);
	}
}
