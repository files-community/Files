// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class JumpListService
{
	private JumpListService() { }

	public static IAsyncAction RefreshJumpListAsync() => ServerAsync.Run(token => JumpListCatalog.RefreshAsync().WaitAsync(token));

	public static IAsyncAction AddJumpListFolderAsync(string path) => ServerAsync.Run(token => JumpListCatalog.AddAsync(path).WaitAsync(token));

	public static IAsyncAction RemoveJumpListFolderAsync(string path) => ServerAsync.Run(token => JumpListCatalog.RemoveAsync(path).WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<string>> GetJumpListFoldersAsync() => ServerAsync.Run(token => JumpListCatalog.GetAsync().WaitAsync(token));
}
