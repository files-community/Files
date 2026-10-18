// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Shell;

public sealed partial class ContextMenu : Win32ContextMenu, IDisposable
{
	private readonly Server.ContextMenuSession session;
	private readonly Func<string?, bool>? filter;
	private readonly Dictionary<List<Win32ContextMenuItem>, int> submenus = [];
	private int disposed;

	public List<string> ItemsPath { get; }

	private ContextMenu(Server.ContextMenuSession session, IEnumerable<string> paths, Func<string?, bool>? filter)
	{
		this.session = session;
		this.filter = filter;
		ItemsPath = paths.ToList();
		Items = Convert(session.GetItems());
	}

	private List<Win32ContextMenuItem> Convert(IReadOnlyList<Server.MenuEntryInfo> entries)
	{
		var result = new List<Win32ContextMenuItem>();
		foreach (var entry in entries)
		{
			if ((MENU_ITEM_TYPE)entry.Type == MENU_ITEM_TYPE.MFT_STRING && filter is not null && (filter(entry.Command) || filter(entry.Label)))
				continue;
			var icon = entry.Icon;
			var item = new ContextMenuItem
			{
				ID = entry.Id,
				Type = (MENU_ITEM_TYPE)entry.Type,
				Label = entry.Label,
				CommandString = entry.Command,
				Icon = icon.Length == 0 ? null : icon,
			};
			if (entry.Submenu != 0)
			{
				item.SubItems = [];
				submenus.Add(item.SubItems, entry.Submenu);
			}
			result.Add(item);
		}
		return result;
	}

	public static async Task<ContextMenu?> GetContextMenuForFiles(string?[] paths, uint flags, Func<string?, bool>? itemFilter = null)
	{
		var validPaths = paths.OfType<string>().Where(path => !string.IsNullOrEmpty(path)).ToArray();
		var session = await Server.AppService.GetContextMenuAsync(validPaths, flags);
		if (session is null)
			return null;
		try
		{
			return new ContextMenu(session, validPaths, itemFilter);
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

	public async Task<bool> LoadSubMenu(List<Win32ContextMenuItem> items)
	{
		if (!submenus.Remove(items, out var id))
			return false;
		items.AddRange(Convert(await session.LoadSubmenuAsync(id)));
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
