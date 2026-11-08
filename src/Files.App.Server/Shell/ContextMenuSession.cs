// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class ContextMenuSession : IDisposable
{
	private readonly ContextMenu menu;
	private readonly IReadOnlyList<MenuEntryInfo> entries;
	private readonly Task modernDetails;
	private readonly Dictionary<int, List<Win32ContextMenuItem>> submenus = [];
	private readonly SemaphoreSlim gate = new(1);
	private IReadOnlyList<MenuEntryInfo>? classicEntries;
	private int nextSubmenu;
	private int disposed;
	private bool released;

	internal ContextMenuSession(ContextMenu menu)
	{
		this.menu = menu;
		entries = Convert(menu.Items ?? []);

		modernDetails = menu.LoadModernDetailsAsync();
	}

	public IReadOnlyList<MenuEntryInfo> GetItems() => entries;

	public IAsyncOperation<IReadOnlyList<MenuEntryInfo>> LoadIconsAsync() => ServerAsync.Run<IReadOnlyList<MenuEntryInfo>>(async token =>
	{
		// The apps that take longer are left without icons
		await modernDetails.WaitAsync(ContextMenu.ModernItemsTimeout, token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		token.ThrowIfCancellationRequested();
		return (menu.Items ?? []).Select((item, index) => new MenuEntryInfo(item, entries[index].Submenu)).ToList().AsReadOnly();
	});

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
		List<Win32ContextMenuItem>? items;
		await gate.WaitAsync(token);
		try
		{
			ObjectDisposedException.ThrowIf(released, this);
			submenus.Remove(id, out items);
		}
		finally
		{
			gate.Release();
		}

		if (items is null)
			return new List<MenuEntryInfo>().AsReadOnly();

		await menu.LoadSubMenu(items).WaitAsync(token);
		await gate.WaitAsync(token);
		try
		{
			ObjectDisposedException.ThrowIf(released, this);
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
			ObjectDisposedException.ThrowIf(released, this);
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
			ObjectDisposedException.ThrowIf(released, this);
			return await menu.InvokeVerb(verb);
		}
		finally
		{
			gate.Release();
		}
	});

	public IAsyncOperation<IReadOnlyList<MenuEntryInfo>> LoadClassicItemsAsync() => ServerAsync.Run(async token =>
	{
		// Outside the gate, so it doesn't hold up invoking the items already shown
		var items = await menu.LoadClassicItemsAsync().WaitAsync(token);
		await gate.WaitAsync(token);
		try
		{
			ObjectDisposedException.ThrowIf(released, this);
			return classicEntries ??= Convert(items);
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
		// Operations requested before Dispose complete first
		await gate.WaitAsync();
		try
		{
			released = true;
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
