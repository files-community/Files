// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;

namespace Files.App.Services
{
	internal sealed class QuickAccessService : IQuickAccessService
	{
		private static readonly TimeSpan UnpinSettleTimeout = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan ReconciliationTimeout = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan PinStatePollInterval = TimeSpan.FromMilliseconds(200);

		private static readonly ConcurrentDictionary<string, string> normalizedShellPaths = new(StringComparer.OrdinalIgnoreCase);

		public async Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync()
		{
			var folders = await Server.PinnedFoldersService.GetPinnedFoldersAsync();
			return folders.Select(folder => new ShellFileItem
			{
				IsFolder = true,
				FileName = folder.Name,
				FilePath = folder.Path,
				PIDL = folder.Pidl,
				Properties = new() { ["System.Home.IsPinned"] = folder.IsPinned },
			}).ToArray();
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
					if (!ShellStorageFolder.IsShellPath(folderPath) || !await TryPinShellItemAsync(folderPath))
						await ContextMenu.InvokeVerb("pintohome", folderPath);
				}
			}

			Server.PinnedFoldersService.InvalidatePinnedFolders();
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
				folderPaths = GetPinnedPaths(shellItems);

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
					Server.PinnedFoldersService.InvalidatePinnedFolders();
					await App.QuickAccessManager.Model.LoadAsync();
					return false;
				}
			}

			Server.PinnedFoldersService.InvalidatePinnedFolders();
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

		private static Task<bool> TryPinShellItemAsync(string folderPath)
		{
			return STATask.Run(() =>
			{
				using var shellItem = ShellFolderExtensions.GetShellItemFromPathOrPIDL(folderPath);
				using var windowsFile = new WindowsFile(shellItem.IShellItem);
				return windowsFile.TryInvokeContextMenuVerbs(["pintohome"], true).Succeeded;
			}, App.Logger);
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

			if (normalizedShellPaths.TryGetValue(path, out var cachedParsingName))
				return cachedParsingName;

			if (TryGetShellParsingName(path) is { } parsingName)
				return normalizedShellPaths[path] = parsingName;

			return path.StartsWith(@"\SHELL\", StringComparison.OrdinalIgnoreCase)
				? path.Replace(@"\SHELL\", string.Empty, StringComparison.OrdinalIgnoreCase)
				: path;
		}

		private static string? TryGetShellParsingName(string path)
		{
			try
			{
				using var shellItem = ShellFolderExtensions.GetShellItemFromPathOrPIDL(path);
				var parsingName = shellItem?.ParsingName;
				return string.IsNullOrWhiteSpace(parsingName) ? null : parsingName;
			}
			catch (Exception ex) when (ex is COMException or IOException or ArgumentException)
			{
				App.Logger.LogDebug(ex, "Failed to resolve shell path {Path}", path);
				return null;
			}
		}

		private static string[] GetPinnedPaths(IEnumerable<ShellFileItem> shellItems)
		{
			return shellItems
				.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
				.Select(link => link.FilePath!)
				.ToArray();
		}

		private async Task<string[]> GetPinnedFolderPathsAsync()
		{
			Server.PinnedFoldersService.InvalidatePinnedFolders();
			return GetPinnedPaths(await GetPinnedFoldersAsync());
		}

		private async Task<string[]> GetMissingPinnedItemsAsync(IEnumerable<string> desiredItems)
		{
			var normalizedCurrentPinned = BuildNormalizedPathSet(await GetPinnedFolderPathsAsync());
			return desiredItems
				.Where(x => !normalizedCurrentPinned.Contains(NormalizeQuickAccessPath(x)))
				.ToArray();
		}

		private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
		{
			var deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
			while (!await condition() && Environment.TickCount64 < deadline)
				await Task.Delay(PinStatePollInterval);
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
			var missingItems = await GetMissingPinnedItemsAsync(desiredItems);
			if (missingItems.Length == 0)
				return;

			await PinToSidebarAsync(missingItems, doUpdateQuickAccessWidget: false, force: true);
			await WaitUntilAsync(async () => (await GetMissingPinnedItemsAsync(desiredItems)).Length == 0, ReconciliationTimeout);
		}

		public async Task SaveAsync(string[] items)
		{
			var desiredItems = items
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();

			if (desiredItems.SequenceEqual(App.QuickAccessManager.Model.PinnedFolders, StringComparer.OrdinalIgnoreCase))
				return;

			var pinnedFolders = (await App.ServerSession).PinnedFolders;
			pinnedFolders.BeginUpdate();
			try
			{
				using (App.QuickAccessManager.Model.SuspendSync())
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
				}

				await App.QuickAccessManager.Model.LoadAsync();
			}
			finally
			{
				SafetyExtensions.IgnoreExceptions(pinnedFolders.EndUpdate, App.Logger);
			}
		}
	}
}
