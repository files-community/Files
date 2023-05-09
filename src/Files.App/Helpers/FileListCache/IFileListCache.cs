// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers.FileListCache
{
	internal interface IFileListCache
	{
		internal ValueTask<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken);

		internal ValueTask SaveFileDisplayNameToCache(string path, string displayName);
	}
}