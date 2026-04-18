// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Microsoft.Extensions.Logging;

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

							dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, [pathForShell]);
							if (f2 != null)
							{
								dynamic? fi = f2.Self;
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

		private async Task UnpinFromSidebarAsync(string[] folderPaths, bool doUpdateQuickAccessWidget)
		{
			folderPaths = NormalizeAndDeduplicatePaths(folderPaths);

			if (folderPaths.Length == 0)
			{
				folderPaths = NormalizeAndDeduplicatePaths((await GetPinnedFoldersAsync())
					.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
					.Select(link => link.FilePath)
					.ToArray());
			}

			try
			{
				Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
				if (shellAppType == null)
					return;

				object? shell = Activator.CreateInstance(shellAppType);
				dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, [$"shell:{guid}"]);
				if (f2 == null)
					return;

				List<string> pathsToUnpin = new();
				var normalizedTargetPaths = BuildNormalizedPathSet(folderPaths);

				foreach (dynamic? fi in f2.Items())
				{
					string pathStr = (string)fi.Path;
					var normalizedPathStr = NormalizeQuickAccessPath(pathStr);
					bool shouldUnpin = normalizedTargetPaths.Contains(normalizedPathStr);

					if (!shouldUnpin && ShellStorageFolder.IsShellPath(pathStr))
					{
						var folder = await ShellStorageFolder.FromPathAsync(pathStr);
						var path = folder?.Path;

						if (!string.IsNullOrWhiteSpace(path))
							shouldUnpin = normalizedTargetPaths.Contains(NormalizeQuickAccessPath(path));
					}

					if (shouldUnpin)
					{
						pathsToUnpin.Add(pathStr);
					}
				}

				if (pathsToUnpin.Count > 0)
				{
					var normalizedPathsToUnpin = BuildNormalizedPathSet(pathsToUnpin);
					await STATask.Run(() =>
					{
						Type? shellAppTypeSTA = Type.GetTypeFromProgID("Shell.Application");
						if (shellAppTypeSTA == null) return;
						object? shellSTA = Activator.CreateInstance(shellAppTypeSTA);
						dynamic? f2STA = shellAppTypeSTA.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shellSTA, [$"shell:{guid}"]);
						if (f2STA == null) return;

						foreach (dynamic? fi in f2STA.Items())
						{
							string pathStr = (string)fi.Path;
							if (normalizedPathsToUnpin.Contains(NormalizeQuickAccessPath(pathStr)))
							{
								var unpinned = TryInvokeShellVerb(fi, "unpinfromhome", pathStr);
								if (!unpinned && ShellStorageFolder.IsShellPath(pathStr))
									TryInvokeShellVerb(fi, "remove", pathStr);
							}
						}
					}, App.Logger);
				}
			}
			finally
			{
				await App.QuickAccessManager.Model.LoadAsync();
				if (doUpdateQuickAccessWidget)
					App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, false));
			}
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

		private static bool TryInvokeShellVerb(dynamic? shellItem, string verb, string path)
		{
			if (shellItem is null)
				return false;

			try
			{
				shellItem.InvokeVerb(verb);
				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogDebug(ex, "Failed to invoke shell verb {Verb} for {Path}", verb, path);
				return false;
			}
		}

		private static string[] NormalizeAndDeduplicatePaths(IEnumerable<string>? paths)
		{
			if (paths is null)
				return [];

			List<string> result = [];
			HashSet<string> normalizedSet = new(StringComparer.OrdinalIgnoreCase);

			foreach (var path in paths.Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				var normalizedPath = NormalizeQuickAccessPath(path);
				if (normalizedSet.Add(normalizedPath))
					result.Add(path);
			}

			return result.ToArray();
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
			catch
			{
				// fallback to raw PIDL strings
			}

			return path.StartsWith(@"\\SHELL\", StringComparison.OrdinalIgnoreCase)
				? path.Replace(@"\\SHELL\", string.Empty, StringComparison.OrdinalIgnoreCase)
				: path;
		}

		private async Task<string[]> GetPinnedFolderPathsAsync()
		{
			return (await GetPinnedFoldersAsync())
				.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
				.Select(link => link.FilePath)
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
						// prevents wait deadlocks if the FileSystemWatcher 
						// randomly swallows a background COM completion event
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
				var itemsToUnpin = await GetPinnedFolderPathsAsync();

				if (itemsToUnpin.Length > 0)
				{
					await UnpinFromSidebarAsync(itemsToUnpin, false);
					await WaitUntilAsync(async () =>
					{
						var currentPinned = await GetPinnedFolderPathsAsync();
						var normalizedCurrentPinned = BuildNormalizedPathSet(currentPinned);

						return !itemsToUnpin.Any(x => normalizedCurrentPinned.Contains(NormalizeQuickAccessPath(x)));
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
