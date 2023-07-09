// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Utils.Storage
{
	internal interface IStorageCacheController
	{
		public ValueTask<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken);

		public ValueTask SaveFileDisplayNameToCache(string path, string displayName);
	}
}