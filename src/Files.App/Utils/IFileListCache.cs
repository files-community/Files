// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils
{
	internal interface IFileListCache
	{
		public ValueTask<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken);

		public ValueTask SaveFileDisplayNameToCache(string path, string displayName);
	}
}
