// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;
using Windows.Storage.Streams;

namespace Files.App.Server;

public sealed partial class IconService
{
	private IconService() { }

	public static IAsyncOperation<IBuffer> GetGenericIconAsync(string extension, bool folder, int size) => ServerAsync.Run(token => IconCache.GetGenericAsync(extension, folder, size).WaitAsync(token));

	public static IAsyncOperation<IBuffer> GetIconAsync(string path, int size, bool folder, uint options, bool overlay) => ServerAsync.Run(token => IconCache.GetAsync(path, size, folder, options, overlay).WaitAsync(token));
}
