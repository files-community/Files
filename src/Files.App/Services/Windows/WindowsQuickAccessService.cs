// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;
using System.Runtime.InteropServices;

namespace Files.App.Services
{
	internal sealed class QuickAccessService : IQuickAccessService
	{
		// Quick access shell folder (::{679f85cb-0220-4080-b29b-5540cc05aab6}) contains recent files
		// which are unnecessary for getting pinned folders, so we use frequent places shell folder instead.
		private readonly static string guid = "::{3936e9e4-d92c-4eee-a85a-bc16d5ea0819}";
		private static readonly TimeSpan UnpinSettleTimeout = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan ReconciliationTimeout = TimeSpan.FromSeconds(5);

		public async Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync()
		{
			var result = (await Win32Helper.GetShellFolderAsync(guid, false, true, 0, int.MaxValue, "System.Home.IsPinned")).Enumerate
				.Where(link => link.IsFolder);
			return result;
		}

		public Task PinToSidebarAsync(string folderPath) => PinToSidebarAsync(new[] { folderPath });

		public Task PinToSidebarAsync(string[] folderPaths) => PinToSidebarAsync(folderPaths, true);

		private async Task PinToSidebarAsync(string[] folderPaths, bool doUpdateQuickAccessWidget, bool force = false)
		{
			foreach (string folderPath in folderPaths)
			{
				// make sure that the item has not yet been pinned
				// the verb 'pintohome' is for both adding and removing
				if (force || !IsItemPinned(folderPath))
				{
					if (ShellStorageFolder.IsShellPath(folderPath))
					{
						bool success = false;
						await STATask.Run(() =>
						{
							Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
							if (shellAppType == null)
								return;

							object? shell = Activator.CreateInstance(shellAppType);
							string pathForShell = folderPath;
							if (folderPath.StartsWith(@"\\SHELL\", StringComparison.OrdinalIgnoreCase))
							{
								using var shellItem = ShellFolderExtensions.GetShellItemFromPathOrPIDL(folderPath);
								if (shellItem is null)
									return;
								pathForShell = shellItem.ParsingName ?? folderPath;
							}

							object? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, [pathForShell]);
							if (f2 != null)
							{
								object? fi = f2.GetType().InvokeMember("Self", System.Reflection.BindingFlags.GetProperty, null, f2, []);
								success = TryInvokeShellVerb(fi, "pintohome", pathForShell);
							}
						}, App.Logger);

						if (!success)
						{
							await ContextMenu.InvokeVerb("pintohome", folderPath);
						}
					}
					else
					{
						await ContextMenu.InvokeVerb("pintohome", folderPath);
					}
				}
			}

			await App.QuickAccessManager.Model.LoadAsync();
			if (doUpdateQuickAccessWidget)
				App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, true));
		}

		public Task UnpinFromSidebarAsync(string folderPath) => UnpinFromSidebarAsync(new[] { folderPath });

		public Task UnpinFromSidebarAsync(string[] folderPaths) => UnpinFromSidebarAsync(folderPaths, true);

		private async Task<bool> UnpinFromSidebarAsync(string[] folderPaths, bool doUpdateQuickAccessWidget)
		{
			ShellFileItem[] shellItems = [.. await GetPinnedFoldersAsync()];

			if (folderPaths.Length == 0)
				folderPaths = shellItems
					.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
					.Select(link => link.FilePath!).ToArray();

			foreach (ShellFileItem shellItem in shellItems)
			{
				string pathStr = shellItem.FilePath
					?? throw new InvalidOperationException("The Windows Shell Home namespace returned an item without a path.");
				bool shouldUnpin = folderPaths.Contains(pathStr);

				if (ShellStorageFolder.IsShellPath(pathStr))
				{
					var folder = await ShellStorageFolder.FromPathAsync(pathStr);
					var path = folder?.Path;

					shouldUnpin = shouldUnpin || path is not null &&
						(folderPaths.Contains(path) ||
						(path.StartsWith(@"\\SHELL\\") && folderPaths.Any(x => x.StartsWith(@"\\SHELL\\"))));
				}

				if (!shouldUnpin)
					continue;

				byte[] pidl = shellItem.PIDL
					?? throw new InvalidOperationException("The Windows Shell Home namespace returned an item without a PIDL.");

				var result = await STATask.Run(() =>
				{
					using var item = ShellItem.Open(new ShellPidl(pidl));
					using var windowsFile = new WindowsFile(item.IShellItem);
					return windowsFile.TryInvokeContextMenuVerbs(["unpinfromhome", "remove"], true);
				}, App.Logger);

				if (result.Failed)
				{
					await App.QuickAccessManager.Model.LoadAsync();
					return false;
				}
			}

			await App.QuickAccessManager.Model.LoadAsync();
			if (doUpdateQuickAccessWidget)
				App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, false));

			return true;
		}

		public bool IsItemPinned(string folderPath)
		{
			if (App.QuickAccessManager.Model.PinnedFolders.Contains(folderPath, StringComparer.OrdinalIgnoreCase))
				return true;

			if (!ShellStorageFolder.IsShellPath(folderPath))
				return false;

			var normalizedPath = NormalizeQuickAccessPath(folderPath);
			return App.QuickAccessManager.Model.PinnedFolders
				.Any(x => string.Equals(NormalizeQuickAccessPath(x), normalizedPath, StringComparison.OrdinalIgnoreCase));
		}

		private static bool TryInvokeShellVerb(object? shellItem, string verb, string path)
		{
			if (shellItem is null)
				return false;

			return Files.Shared.Extensions.SafetyExtensions.IgnoreExceptions(() =>
			{
				shellItem.GetType().InvokeMember("InvokeVerb", System.Reflection.BindingFlags.InvokeMethod, null, shellItem, [verb]);
				return true;
			}, App.Logger, typeof(Exception));
		}

		private static HashSet<string> BuildNormalizedPathSet(IEnumerable<string> paths)
		{
			return new HashSet<string>(
				paths
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.Select(NormalizeQuickAccessPath),
				StringComparer.OrdinalIgnoreCase);
		}

		private static string NormalizeQuickAccessPath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return string.Empty;

			if (!ShellStorageFolder.IsShellPath(path))
				return path;

			try
			{
				using var shellItem = ShellFolderExtensions.GetShellItemFromPathOrPIDL(path);
				var parsingName = shellItem?.ParsingName;
				if (!string.IsNullOrWhiteSpace(parsingName))
					return parsingName;
			}
			catch (COMException ex)
			{
				App.Logger.LogDebug(ex, "Failed to resolve shell path {Path}", path);
			}

			return path.StartsWith(@"\\SHELL\", StringComparison.OrdinalIgnoreCase)
				? path.Replace(@"\\SHELL\", string.Empty, StringComparison.OrdinalIgnoreCase)
				: path;
		}

		private async Task<string[]> GetPinnedFolderPathsAsync()
		{
			return (await GetPinnedFoldersAsync())
				.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
				.Select(link => link.FilePath!)
				.ToArray();
		}

		private async Task<string[]> GetMissingPinnedItemsAsync(IEnumerable<string> desiredItems)
		{
			var normalizedCurrentPinned = BuildNormalizedPathSet(await GetPinnedFolderPathsAsync());
			return desiredItems
				.Where(x => !normalizedCurrentPinned.Contains(NormalizeQuickAccessPath(x)))
				.ToArray();
		}

		private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
		{
			if (await condition())
				return true;

			// Quick Access state is saved by the OS into f01b...automaticDestinations-ms
			var automaticDestinationsPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent", "AutomaticDestinations");
			
			if (!Directory.Exists(automaticDestinationsPath))
				return await PollWaitAsync(condition, timeout, TimeSpan.FromMilliseconds(200));

			try
			{
				using var cts = new CancellationTokenSource(timeout);
				using var watcher = new FileSystemWatcher(automaticDestinationsPath, "f01b4d95cf55d32a.automaticDestinations-ms")
				{
					NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName
				};

				using var semaphore = new SemaphoreSlim(0);
				void OnChanged(object sender, FileSystemEventArgs e)
				{
					try
					{
						semaphore.Release();
					}
					catch (ObjectDisposedException)
					{

					}
				}

				watcher.Changed += OnChanged;
				watcher.Created += OnChanged;
				watcher.Deleted += OnChanged;

				try
				{
					watcher.EnableRaisingEvents = true;

					while (!cts.IsCancellationRequested)
					{
						if (await condition())
							return true;

						try
						{
							// Bounded wait so a missed watcher event can't stall the loop
							await semaphore.WaitAsync(TimeSpan.FromMilliseconds(400), cts.Token);
						}
						catch (OperationCanceledException)
						{
							break;
						}
					}
				}
				finally
				{
					watcher.Changed -= OnChanged;
					watcher.Created -= OnChanged;
					watcher.Deleted -= OnChanged;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "FileSystemWatcher failed to initialize for automaticDestinations-ms. Falling back to polling.");
				return await PollWaitAsync(condition, timeout, TimeSpan.FromMilliseconds(200));
			}

			return await condition();
		}

		private static async Task<bool> PollWaitAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan pollInterval)
		{
			using var cts = new CancellationTokenSource(timeout);
			while (!cts.IsCancellationRequested)
			{
				if (await condition())
					return true;

				try
				{
					await Task.Delay(pollInterval, cts.Token);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
			
			return await condition();
		}

		/// <summary>
		/// Returns how many leading items of <paramref name="desiredItems"/> already appear in
		/// <paramref name="currentPinned"/> in the same relative order.
		/// </summary>
		private static int GetStablePrefixLength(string[] desiredItems, string[] currentPinned)
		{
			var desiredNormalized = desiredItems.Select(NormalizeQuickAccessPath).ToArray();

			int matched = 0;
			foreach (var pinnedPath in currentPinned)
			{
				if (matched < desiredNormalized.Length &&
					string.Equals(NormalizeQuickAccessPath(pinnedPath), desiredNormalized[matched], StringComparison.OrdinalIgnoreCase))
					matched++;
			}

			return matched;
		}

		private async Task ReconcilePinsAsync(string[] desiredItems)
		{
			await WaitUntilAsync(async () =>
			{
				var missingItems = await GetMissingPinnedItemsAsync(desiredItems);
				if (missingItems.Length == 0)
					return true;

				await PinToSidebarAsync(missingItems, false, force: true);
				return false;
			}, ReconciliationTimeout);
		}

		public async Task SaveAsync(string[] items)
		{
			var desiredItems = items
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();

			if (desiredItems.SequenceEqual(App.QuickAccessManager.Model.PinnedFolders, StringComparer.OrdinalIgnoreCase))
				return;

			if (App.QuickAccessManager.PinnedItemsWatcher is not null)
				App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = false;

			try
			{
				var currentPinned = await GetPinnedFolderPathsAsync();

				// Pinning appends, so only items outside the longest in-order prefix need the unpin/repin cycle
				var stableCount = GetStablePrefixLength(desiredItems, currentPinned);
				var stableSet = BuildNormalizedPathSet(desiredItems.Take(stableCount));
				var itemsToUnpin = currentPinned
					.Where(x => !stableSet.Contains(NormalizeQuickAccessPath(x)))
					.ToArray();

				if (itemsToUnpin.Length > 0)
				{
					await UnpinFromSidebarAsync(itemsToUnpin, false);
					await WaitUntilAsync(async () =>
					{
						var remainingPinned = BuildNormalizedPathSet(await GetPinnedFolderPathsAsync());

						return !itemsToUnpin.Any(x => remainingPinned.Contains(NormalizeQuickAccessPath(x)));
					}, UnpinSettleTimeout);
				}

				await ReconcilePinsAsync(desiredItems);
				await App.QuickAccessManager.Model.LoadAsync();
			}
			finally
			{
				if (App.QuickAccessManager.PinnedItemsWatcher is not null)
					App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = true;
			}
		}
	}
}
