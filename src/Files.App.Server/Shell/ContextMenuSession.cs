// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class ContextMenuSession : IDisposable
{
	private readonly ContextMenu menu;
	private readonly IReadOnlyList<MenuEntryInfo> entries;
	private readonly Dictionary<int, List<Win32ContextMenuItem>> submenus = [];
	private readonly SemaphoreSlim gate = new(1);
	private int nextSubmenu;
	private int disposed;

	internal ContextMenuSession(ContextMenu menu)
	{
		this.menu = menu;
		entries = Convert(menu.Items ?? []);
	}

	public IReadOnlyList<MenuEntryInfo> GetItems() => entries;

	private IReadOnlyList<MenuEntryInfo> Convert(List<Win32ContextMenuItem> items)
	{
		var result = new List<MenuEntryInfo>();
		foreach (var item in items)
		{
			var submenu = 0;
			if (item.SubItems is not null)
			{
				submenu = ++nextSubmenu;
				submenus.Add(submenu, item.SubItems);
			}
			result.Add(new MenuEntryInfo(item, submenu));
		}
		return result.AsReadOnly();
	}

	public IAsyncOperation<IReadOnlyList<MenuEntryInfo>> LoadSubmenuAsync(int id) => ServerAsync.Run(async token =>
	{
		await gate.WaitAsync(token);
		try
		{
			ObjectDisposedException.ThrowIf(disposed != 0, this);
			if (!submenus.Remove(id, out var items))
				return new List<MenuEntryInfo>().AsReadOnly();
			await menu.LoadSubMenu(items);
			return Convert(items);
		}
		finally
		{
			gate.Release();
		}
	});

	public IAsyncOperation<bool> InvokeItemAsync(int id, string workingDirectory) => ServerAsync.Run(async token =>
	{
		await gate.WaitAsync(token);
		try
		{
			ObjectDisposedException.ThrowIf(disposed != 0, this);
			// WinRT represents null strings as empty; the shell needs null for an omitted directory.
			return await menu.InvokeItem(id, string.IsNullOrEmpty(workingDirectory) ? null : workingDirectory);
		}
		finally
		{
			gate.Release();
		}
	});

	public IAsyncOperation<bool> InvokeVerbAsync(string verb) => ServerAsync.Run(async token =>
	{
		await gate.WaitAsync(token);
		try
		{
			ObjectDisposedException.ThrowIf(disposed != 0, this);
			return await menu.InvokeVerb(verb);
		}
		finally
		{
			gate.Release();
		}
	});

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) != 0)
			return;
		_ = DisposeAsync();
		GC.SuppressFinalize(this);
	}

	private async Task DisposeAsync()
	{
		await gate.WaitAsync();
		try
		{
			menu.Dispose();
		}
		catch (Exception ex)
		{
			Program.Logger.LogDebug(ex, "Could not release a shell context menu.");
		}
		finally
		{
			gate.Release();
		}
	}
}
