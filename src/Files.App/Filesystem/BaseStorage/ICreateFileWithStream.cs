// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using Windows.Foundation;
using Windows.Storage;

namespace Files.App.Filesystem.StorageItems
{
	public interface ICreateFileWithStream
	{
		IAsyncOperation<BaseStorageFile> CreateFileAsync(Stream contents, string desiredName);

		IAsyncOperation<BaseStorageFile> CreateFileAsync(Stream contents, string desiredName, CreationCollisionOption options);
	}
}
