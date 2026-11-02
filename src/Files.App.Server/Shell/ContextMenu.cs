// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Files.App.Server
{
	/// <summary>
	/// Provides a helper for Win32 context menus.
	/// </summary>
	internal sealed partial class ContextMenu : Win32ContextMenu, IDisposable
	{
		// Packaged app and cloud provider commands that take longer are left out of the menu, including its classic items
		internal static readonly TimeSpan ModernItemsTimeout = TimeSpan.FromSeconds(3);

		// Apps still loading past the timeout, which later menus leave out until they're done
		private static readonly ConcurrentDictionary<string, Task> stalledGroups = new(StringComparer.Ordinal);

		private readonly ContextMenuWorkerPool.Worker worker;
		private readonly Task<ClassicContextMenuBackend?> classicMenu;
		private readonly List<ModernMenu> modernMenus;
		private bool disposedValue;

		private ContextMenu(ContextMenuWorkerPool.Worker worker, Task<ClassicContextMenuBackend?> classicMenu, List<ModernMenu>? modernMenus = null)
		{
			this.worker = worker;
			this.classicMenu = classicMenu;
			this.modernMenus = modernMenus ?? [];
		}

		public static async Task<bool> InvokeVerb(string verb, params string?[] filePaths)
		{
			using var contextMenu = await GetContextMenuForFiles(filePaths, PInvoke.CMF_DEFAULTONLY);
			return contextMenu is not null && await contextMenu.InvokeVerb(verb);
		}

		public async Task<bool> InvokeVerb(string? verb)
		{
			if (string.IsNullOrEmpty(verb))
				return false;

			var items = Items ?? throw new InvalidOperationException("The shell context menu has not been initialized.");
			var item = items.FirstOrDefault(x => x.CommandString == verb);
			if (item is not null && item.ID >= 0)
				return await InvokeItem(item.ID);

			var classic = await classicMenu;
			return classic is not null && await InvokeAsync(worker, () => classic.InvokeVerb(verb));
		}

		public async Task<bool> InvokeItem(int itemId, string? workingDirectory = null)
		{
			if (itemId < 0)
				return false;

			if (modernMenus.FirstOrDefault(modern => modern.Menu.HasItem(itemId)) is { } owner)
				return await InvokeAsync(owner.Worker, () => owner.Menu.InvokeItem(itemId));

			var classic = await classicMenu;
			return classic is not null && await InvokeAsync(worker, () => classic.InvokeItem(itemId, workingDirectory));
		}

		private async Task<bool> InvokeAsync(ContextMenuWorkerPool.Worker owner, Func<HRESULT> invoke)
		{
			try
			{
				HRESULT result = await owner.Thread.PostMethod(() =>
				{
					ObjectDisposedException.ThrowIf(disposedValue, this);
					return invoke();
				});
				return result.Succeeded;
			}
			catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
				return false;
			}
		}

		public static async Task<ContextMenu?> GetContextMenuForFiles(string?[] filePathList, uint flags, Func<string?, bool>? itemFilter = null, ContextMenuQueryOptions options = ContextMenuQueryOptions.None)
		{
			string[] paths = [.. filePathList.WhereNotNull().Where(path => !string.IsNullOrEmpty(path))];
			if (paths.Length is 0)
				return null;

			var worker = ContextMenuWorkerPool.Rent();
			var classicMenu = worker.Thread.PostMethod(() => OpenShellItems(paths, shellItems => CreateClassicMenu(shellItems, flags, itemFilter)));
			if (!options.HasFlag(ContextMenuQueryOptions.ModernItems))
				return await CreateClassicOnlyAsync(worker, classicMenu);

			var modernMenus = await LoadModernMenusAsync(paths, options.HasFlag(ContextMenuQueryOptions.Background));
			return new(worker, classicMenu, modernMenus) { Items = [.. modernMenus.SelectMany(modern => modern.Menu.Items)] };
		}

		private static async Task<List<ModernMenu>> LoadModernMenusAsync(string[] paths, bool isBackground)
		{
			var worker = ContextMenuWorkerPool.Rent();
			var groups = worker.Thread.PostMethod(() => OpenShellItems(paths, shellItems => SafetyExtensions.IgnoreExceptions(() => ModernContextMenuBackend.GetGroups(shellItems, isBackground), Program.Logger)));
			if (await Task.WhenAny(groups, Task.Delay(ModernItemsTimeout)) != groups)
			{
				_ = groups.ContinueWith(_ => ContextMenuWorkerPool.Return(worker), TaskScheduler.Default);
				return [];
			}

			ContextMenuWorkerPool.Return(worker);
			var loads = new List<(ContextMenuWorkerPool.Worker Worker, Task<ModernContextMenuBackend?> Menu, string GroupId)>();
			foreach (var (groupIndex, group) in (groups.Result ?? []).Index())
			{
				if (stalledGroups.ContainsKey(group.Key))
					continue;

				var groupWorker = ContextMenuWorkerPool.Rent();
				var load = groupWorker.Thread.PostMethod(() => OpenShellItems(paths, shellItems => new ModernContextMenuBackend(ShellItemArray.Create(shellItems), shellItems, isBackground, group, groupIndex)));
				_ = MarkStalledAsync(group.Key, load);
				loads.Add((groupWorker, load, group.Key));
			}

			await Task.WhenAny(Task.WhenAll(loads.Select(load => load.Menu)), Task.Delay(ModernItemsTimeout));
			var menus = new List<ModernMenu>();
			foreach (var (groupWorker, menu, groupId) in loads)
			{
				if (menu.IsCompletedSuccessfully && menu.Result is { Items.Count: > 0 } loaded)
				{
					menus.Add(new(groupWorker, loaded, groupId));
				}
				else
				{
					_ = ReleaseAsync(groupWorker, () => menu.Result?.Dispose());
				}
			}

			return menus;
		}

		private static async Task MarkStalledAsync(string groupId, Task task)
		{
			if (await Task.WhenAny(task, Task.Delay(ModernItemsTimeout)) == task)
				return;

			stalledGroups.TryAdd(groupId, task);
			await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			stalledGroups.TryRemove(KeyValuePair.Create(groupId, task));
		}

		public static async Task<ContextMenu?> GetContextMenuForFiles(ShellItem[] shellItems, uint flags, Func<string?, bool>? itemFilter = null)
		{
			if (shellItems.Length is 0)
				return null;

			var worker = ContextMenuWorkerPool.Rent();
			return await CreateClassicOnlyAsync(worker, worker.Thread.PostMethod(() => CreateClassicMenu(shellItems, flags, itemFilter)));
		}

		private static async Task<ContextMenu?> CreateClassicOnlyAsync(ContextMenuWorkerPool.Worker worker, Task<ClassicContextMenuBackend?> classicMenu)
		{
			if (await classicMenu is { } classic)
				return new(worker, classicMenu) { Items = classic.Items };

			ContextMenuWorkerPool.Return(worker);
			return null;
		}

		private static T? OpenShellItems<T>(string[] paths, Func<ShellItem[], T?> create) where T : class
		{
			var shellItems = new List<ShellItem>();
			try
			{
				foreach (string path in paths)
					shellItems.Add(ShellPath.Open(path));
				return create([.. shellItems]);
			}
			catch
			{
				return null;
			}
			finally
			{
				foreach (ShellItem item in shellItems)
					item.Dispose();
			}
		}

		private static ClassicContextMenuBackend? CreateClassicMenu(ShellItem[] shellItems, uint flags, Func<string?, bool>? itemFilter)
		{
			try
			{
				return new(ShellItemArray.Create(shellItems), flags, itemFilter);
			}
			catch (COMException)
			{
				return null;
			}
		}

		public static async Task WarmUpQueryContextMenuAsync()
		{
			// The first lookup of packaged app commands in a process is slow, and their servers stay loaded once started
			var packagedCommands = Task.Run(() => SafetyExtensions.IgnoreExceptions(() => ExplorerCommandActivator.PrepareAsync(
				FileExplorerAppExtensions.GetCommands(["Directory", @"Directory\Background", "*"]).Select(registration => registration.ClassId)), Program.Logger));
			using var contextMenu = await GetContextMenuForFiles([Path.GetPathRoot(Environment.SystemDirectory)!], PInvoke.CMF_NORMAL);
			await packagedCommands;
		}

		// Icons and subcommands of the packaged app and cloud provider commands
		public Task LoadModernDetailsAsync()
		{
			foreach (var modern in modernMenus)
			{
				modern.Details = modern.Worker.Thread.PostMethod(() =>
				{
					if (!disposedValue)
						SafetyExtensions.IgnoreExceptions(modern.Menu.LoadDetails, Program.Logger);
				});

				_ = MarkStalledAsync(modern.GroupId, modern.Details);
			}

			return Task.WhenAll(modernMenus.Select(modern => modern.Details));
		}

		public async Task<List<Win32ContextMenuItem>> LoadClassicItemsAsync()
		{
			var classic = await classicMenu;
			ObjectDisposedException.ThrowIf(disposedValue, this);
			if (classic is null)
				return [];

			// Explorer also lists packaged app commands in the classic menu
			return modernMenus.Count == 0 ? classic.Items : [.. classic.Items.Where(item => !modernMenus.Any(modern => modern.Menu.HasCommand(item.CommandString)))];
		}

		public async Task<bool> LoadSubMenu(List<Win32ContextMenuItem> subItems)
		{
			// The details of the app fill in its submenus
			if (modernMenus.FirstOrDefault(modern => modern.Menu.HasSubmenu(subItems)) is { } owner)
			{
				await owner.Details;
				return true;
			}

			// Load classic submenus on demand.
			if (!classicMenu.IsCompletedSuccessfully || await classicMenu is not { } classic || !classic.HasSubmenu(subItems))
				return false;

			try
			{
				return await worker.Thread.PostMethod(() => !disposedValue && classic.LoadSubMenu(subItems));
			}
			catch
			{
				return false;
			}
		}

		private void Dispose(bool disposing)
		{
			if (disposedValue)
				return;

			// Packaged app and cloud provider items are released by their backend, on its worker
			if (disposing && modernMenus.Count == 0 && Items is not null)
			{
				foreach (Win32ContextMenuItem item in Items)
					(item as IDisposable)?.Dispose();
			}

			// Release the shell objects on the workers' own STA threads once pending work completes.
			disposedValue = true;
			_ = ReleaseAsync(worker, () =>
			{
				if (classicMenu.IsCompletedSuccessfully)
					classicMenu.Result?.Dispose();
			});

			foreach (var modern in modernMenus)
				_ = ReleaseAsync(modern.Worker, modern.Menu.Dispose);
		}

		private static async Task ReleaseAsync(ContextMenuWorkerPool.Worker owner, Action release)
		{
			await SafetyExtensions.IgnoreExceptions(() => owner.Thread.PostMethod(release), Program.Logger);
			ContextMenuWorkerPool.Return(owner);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~ContextMenu()
		{
			Dispose(false);
		}

		private sealed class ModernMenu(ContextMenuWorkerPool.Worker worker, ModernContextMenuBackend menu, string groupId)
		{
			public ContextMenuWorkerPool.Worker Worker { get; } = worker;

			public ModernContextMenuBackend Menu { get; } = menu;

			public string GroupId { get; } = groupId;

			// Its icons and subcommands
			public Task Details { get; set; } = Task.CompletedTask;
		}
	}
}
