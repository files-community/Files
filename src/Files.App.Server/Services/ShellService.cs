// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class ShellService
{
	private ShellService() { }

	public static IAsyncOperation<IReadOnlyList<ShellNewEntryInfo>> GetShellNewEntriesAsync() => ServerAsync.Run(token => InitializationCache.ShellNew.GetAsync().WaitAsync(token));

	public static IAsyncOperation<ShellNewEntryInfo?> GetShellNewEntryAsync(string extension) => ServerAsync.Run(async token =>
	{
		var entry = await Task.Run(() => ShellNewMenuHelper.GetNewContextMenuEntryForType(extension), token);
		return entry is null ? null : new ShellNewEntryInfo(entry);
	});

	public static IAsyncOperation<ContextMenuSession?> GetContextMenuAsync([ReadOnlyArray] string[] paths, uint flags) => ServerAsync.Run(async token =>
	{
		var menu = await ContextMenu.GetContextMenuForFiles(paths, flags);
		return menu is null ? null : new ContextMenuSession(menu);
	});
}
