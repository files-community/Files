// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services
{
	internal sealed class QuickAccessService : IQuickAccessService
	{
		// Quick access shell folder (::{679f85cb-0220-4080-b29b-5540cc05aab6}) contains recent files
		// which are unnecessary for getting pinned folders, so we use frequent places shell folder instead.
		private readonly static string guid = "::{3936e9e4-d92c-4eee-a85a-bc16d5ea0819}";

		public async Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync()
		{
			var result = (await Win32Helper.GetShellFolderAsync(guid, false, true, 0, int.MaxValue, "System.Home.IsPinned")).Enumerate
				.Where(link => link.IsFolder);
			return result;
		}

		public Task PinToSidebarAsync(string folderPath) => PinToSidebarAsync(new[] { folderPath });

		public Task PinToSidebarAsync(string[] folderPaths) => PinToSidebarAsync(folderPaths, true);

		private async Task PinToSidebarAsync(string[] folderPaths, bool doUpdateQuickAccessWidget)
		{
			foreach (string folderPath in folderPaths)
			{
				// make sure that the item has not yet been pinned
				// the verb 'pintohome' is for both adding and removing
				if (!IsItemPinned(folderPath))
					await ContextMenu.InvokeVerb("pintohome", folderPath);
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
			return App.QuickAccessManager.Model.PinnedFolders.Contains(folderPath);
		}

		public async Task SaveAsync(string[] items)
		{
			if (Equals(items, App.QuickAccessManager.Model.PinnedFolders.ToArray()))
				return;

			if (App.QuickAccessManager.PinnedItemsWatcher is not null)
				App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = false;

			// Unpin every item that is below this index and then pin them all in order
			if (!await UnpinFromSidebarAsync([], false))
			{
				if (App.QuickAccessManager.PinnedItemsWatcher is not null)
					App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = true;
				return;
			}

			await PinToSidebarAsync(items, false);
			if (App.QuickAccessManager.PinnedItemsWatcher is not null)
				App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = true;

			App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(items, true)
			{
				Reorder = true
			});
		}
	}
}
