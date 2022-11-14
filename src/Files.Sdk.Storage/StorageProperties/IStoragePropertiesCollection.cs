using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.StorageProperties
{
	public interface IStoragePropertiesCollection
	{
		DateTime DateCreated { get; }

		DateTime DateModified { get; }

		ulong? Size { get; }

		Task<IEnumerable<IStorageProperty>?> GetStoragePropertiesAsync(CancellationToken cancellationToken = default);
	}
}
