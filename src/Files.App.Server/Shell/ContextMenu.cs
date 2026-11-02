// Copyright (c) Files Community
// Licensed under the MIT License.

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
		private readonly ContextMenuWorkerPool.Worker worker;
		private readonly Task<ClassicContextMenuBackend?> classicMenu;

		// A worker of their own lets the classic menu load in parallel
		private readonly ContextMenuWorkerPool.Worker? modernWorker;
		private readonly ModernContextMenuBackend? modernMenu;
		private bool disposedValue;

		private ContextMenu(ContextMenuWorkerPool.Worker worker, Task<ClassicContextMenuBackend?> classicMenu, ContextMenuWorkerPool.Worker? modernWorker = null, ModernContextMenuBackend? modernMenu = null)
		{
			this.worker = worker;
			this.classicMenu = classicMenu;
			this.modernWorker = modernWorker;
			this.modernMenu = modernMenu;
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

			if (modernWorker is not null && modernMenu is { } modern && modern.HasItem(itemId))
				return await InvokeAsync(modernWorker, () => modern.InvokeItem(itemId));

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

			var modernWorker = ContextMenuWorkerPool.Rent();
			var isBackground = options.HasFlag(ContextMenuQueryOptions.Background);
			var modernMenu = await modernWorker.Thread.PostMethod(() => OpenShellItems(paths, shellItems => new ModernContextMenuBackend(ShellItemArray.Create(shellItems), shellItems, isBackground)));
			if (modernMenu is null)
			{
				ContextMenuWorkerPool.Return(modernWorker);
				return new(worker, classicMenu) { Items = [] };
			}

			return new(worker, classicMenu, modernWorker, modernMenu) { Items = modernMenu.Items };
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
			// The first lookup of packaged app commands in a process is slow
			var packagedCommands = Task.Run(() => SafetyExtensions.IgnoreExceptions(() => FileExplorerAppExtensions.GetCommands(["Directory"])));
			using var contextMenu = await GetContextMenuForFiles([Path.GetPathRoot(Environment.SystemDirectory)!], PInvoke.CMF_NORMAL);
			await packagedCommands;
		}

		public async Task<List<Win32ContextMenuItem>> LoadClassicItemsAsync()
		{
			var classic = await classicMenu;
			ObjectDisposedException.ThrowIf(disposedValue, this);
			if (classic is null)
				return [];

			// Explorer also lists packaged app commands in the classic menu
			return modernMenu is null ? classic.Items : [.. classic.Items.Where(item => !modernMenu.HasCommand(item.CommandString))];
		}

		public async Task<bool> LoadSubMenu(List<Win32ContextMenuItem> subItems)
		{
			// Only classic submenus load on demand, after the classic items
			if (!classicMenu.IsCompletedSuccessfully || await classicMenu is not { } classic)
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

			if (disposing && Items is not null)
			{
				foreach (Win32ContextMenuItem item in Items)
					(item as IDisposable)?.Dispose();
			}

			// Release the shell objects on the workers' own STA threads. The message queues are FIFO, so cleanup
			// completes after the menus' creation and before work posted by the next renter.
			disposedValue = true;
			_ = SafetyExtensions.IgnoreExceptions(() => worker.Thread.PostMethod(() =>
			{
				if (classicMenu.IsCompletedSuccessfully)
					classicMenu.Result?.Dispose();
			}), Program.Logger);
			ContextMenuWorkerPool.Return(worker);

			if (modernWorker is not null)
			{
				_ = SafetyExtensions.IgnoreExceptions(() => modernWorker.Thread.PostMethod(() => modernMenu?.Dispose()), Program.Logger);
				ContextMenuWorkerPool.Return(modernWorker);
			}
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
	}
}
