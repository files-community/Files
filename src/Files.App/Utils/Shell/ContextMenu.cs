// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Shell;

public sealed partial class ContextMenu : Win32ContextMenu, IDisposable
{
	private readonly Server.ContextMenuSession session;
	private readonly Func<string?, bool>? filter;
	private readonly Dictionary<List<Win32ContextMenuItem>, (int Id, Func<string?, bool>? Filter)> submenus = [];
	private int disposed;

	public List<string> ItemsPath { get; }

	private ContextMenu(Server.ContextMenuSession session, IEnumerable<string> paths, List<ContextMenuItem> entries, Func<string?, bool>? filter, bool hasModernItems)
	{
		this.session = session;
		this.filter = filter;
		ItemsPath = paths.ToList();

		// The filter only applies to classic items
		Items = Convert(entries, hasModernItems ? null : filter);
	}

	private List<Win32ContextMenuItem> Convert(List<ContextMenuItem> entries, Func<string?, bool>? itemFilter)
	{
		var result = new List<Win32ContextMenuItem>();
		foreach (var item in entries)
		{
			if (item.Type == MENU_ITEM_TYPE.MFT_STRING && itemFilter is not null && (itemFilter(item.CommandString) || itemFilter(item.Label)))
				continue;
			if (item.SubmenuId != 0)
			{
				item.SubItems = [];
				submenus.Add(item.SubItems, (item.SubmenuId, itemFilter));
			}
			result.Add(item);
		}
		return result;
	}

	private static async Task<List<ContextMenuItem>> ReadItemsAsync(Func<Task<IReadOnlyList<Server.MenuEntryInfo>>> getEntries)
	{
		// Collection enumeration and DTO getters are RPCs, even after an async query completes.
		await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
		var entries = await getEntries().ConfigureAwait(false);
		var result = new List<ContextMenuItem>();
		foreach (var entry in entries)
		{
			var icon = entry.Icon;
			var flags = entry.Flags;
			result.Add(new ContextMenuItem
			{
				ID = entry.Id,
				Type = (MENU_ITEM_TYPE)entry.Type,
				Label = entry.Label,
				CommandString = entry.Command,
				Icon = icon.Length == 0 ? null : icon,
				SubmenuId = entry.Submenu,
				IsEnabled = !flags.HasFlag(Server.MenuEntryFlags.Disabled),
				IsChecked = flags.HasFlag(Server.MenuEntryFlags.Checked),
				IsToggle = flags.HasFlag(Server.MenuEntryFlags.Toggle),
			});
		}
		return result;
	}

	public static async Task<ContextMenu?> GetContextMenuForFiles(string?[] paths, uint flags, Func<string?, bool>? itemFilter = null, Server.ContextMenuQueryOptions options = Server.ContextMenuQueryOptions.None)
	{
		var validPaths = paths.OfType<string>().Where(path => !string.IsNullOrEmpty(path)).ToArray();
		var session = await Server.ShellService.GetContextMenuAsync(validPaths, flags, options);
		if (session is null)
			return null;
		try
		{
			var entries = await ReadItemsAsync(() => Task.FromResult(session.GetItems()));
			return new ContextMenu(session, validPaths, entries, itemFilter, options.HasFlag(Server.ContextMenuQueryOptions.ModernItems));
		}
		catch
		{
			session.Dispose();
			throw;
		}
	}

	public static Task<ContextMenu?> GetContextMenuForFiles(ShellItem[] items, uint flags, Func<string?, bool>? itemFilter = null)
		=> GetContextMenuForFiles(items.Select(item => item.ParsingName ?? $@"\\SHELL\{string.Join("\\", item.PIDL.Select(segment => System.Convert.ToBase64String(segment.GetBytes()).Replace('/', '_')))}").ToArray(), flags, itemFilter);

	public static async Task<bool> InvokeVerb(string verb, params string?[] paths)
	{
		using var menu = await GetContextMenuForFiles(paths, PInvoke.CMF_DEFAULTONLY);
		return menu is not null && await menu.InvokeVerb(verb);
	}

	public Task<bool> InvokeVerb(string? verb) => string.IsNullOrEmpty(verb)
		? Task.FromResult(false) : InvokeAsync(async () => await session.InvokeVerbAsync(verb));

	public Task<bool> InvokeItem(int itemId, string? workingDirectory = null)
		=> InvokeAsync(async () => await session.InvokeItemAsync(itemId, workingDirectory ?? string.Empty));

	private static async Task<bool> InvokeAsync(Func<Task<bool>> invoke)
	{
		var windows = Win32Helper.GetDesktopWindows();
		var succeeded = await SafetyExtensions.IgnoreExceptions(invoke, App.Logger);
		if (succeeded)
			Win32Helper.BringToForeground(windows);
		return succeeded;
	}

	public async Task<List<Win32ContextMenuItem>> LoadClassicItemsAsync()
		=> Convert(await ReadItemsAsync(() => session.LoadClassicItemsAsync().AsTask()), filter);

	// Same order as Items
	public async Task<List<byte[]?>> LoadIconsAsync()
		=> [.. (await ReadItemsAsync(() => session.LoadIconsAsync().AsTask())).Select(item => item.Icon)];

	public async Task<bool> LoadSubMenu(List<Win32ContextMenuItem> items)
	{
		if (!submenus.Remove(items, out var submenu))
			return false;
		items.AddRange(Convert(await ReadItemsAsync(() => session.LoadSubmenuAsync(submenu.Id).AsTask()), submenu.Filter));
		return true;
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) != 0)
			return;
		foreach (var item in Items ?? [])
			(item as IDisposable)?.Dispose();
		SafetyExtensions.IgnoreExceptions(session.Dispose, App.Logger);
	}
}
