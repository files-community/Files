// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class FileTagsService
{
	private FileTagsService() { }

	public static IAsyncOperation<IReadOnlyList<TaggedFileInfo>> GetTaggedFilesAsync() => ServerAsync.Run(token => TagCatalog.Items.GetAsync().WaitAsync(token));
}
