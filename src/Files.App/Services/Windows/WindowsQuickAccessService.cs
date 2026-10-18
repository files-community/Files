// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services
{
	internal sealed class QuickAccessService : IQuickAccessService
	{
		public async Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync()
		{
			var folders = await Server.AppService.GetPinnedFoldersAsync();
			return folders.Select(folder => new ShellFileItem
			{
				IsFolder = true,
				FileName = folder.Name,
				FilePath = folder.Path,
				PIDL = folder.Pidl,
				Properties = new() { ["System.Home.IsPinned"] = folder.IsPinned },
			});
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

			Server.AppService.InvalidatePinnedFolders();
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
					Server.AppService.InvalidatePinnedFolders();
					await App.QuickAccessManager.Model.LoadAsync();
					return false;
				}
			}

			Server.AppService.InvalidatePinnedFolders();
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

			// Unpin every item that is below this index and then pin them all in order
			if (!await UnpinFromSidebarAsync([], false))
			{
				return;
			}

			await PinToSidebarAsync(items, false);

			App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(items, true)
			{
				Reorder = true
			});
		}
	}
}
