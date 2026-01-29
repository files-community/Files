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

		private async Task UnpinFromSidebarAsync(string[] folderPaths, bool doUpdateQuickAccessWidget)
		{
			Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
			object? shell = Activator.CreateInstance(shellAppType);
			dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, [$"shell:{guid}"]);

			if (folderPaths.Length == 0)
				folderPaths = (await GetPinnedFoldersAsync())
					.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
					.Select(link => link.FilePath).ToArray();

			foreach (dynamic? fi in f2.Items())
			{
				string pathStr = (string)fi.Path;

				if (ShellStorageFolder.IsShellPath(pathStr))
				{
					var folder = await ShellStorageFolder.FromPathAsync(pathStr);
					var path = folder?.Path;

					if (path is not null &&
						(folderPaths.Contains(path) ||
						(path.StartsWith(@"\\SHELL\\") && folderPaths.Any(x => x.StartsWith(@"\\SHELL\\")))))
					{
						await STATask.Run(async () =>
						{
							fi.InvokeVerb("unpinfromhome");
						}, App.Logger);
						continue;
					}
				}

				if (folderPaths.Contains(pathStr))
				{
					await STATask.Run(async () =>
					{
						fi.InvokeVerb("unpinfromhome");
					}, App.Logger);
				}
			}

			await App.QuickAccessManager.Model.LoadAsync();
			if (doUpdateQuickAccessWidget)
				App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, false));
		}

		public bool IsItemPinned(string folderPath)
		{
			return App.QuickAccessManager.Model.PinnedFolders.Contains(folderPath);
		}

		public async Task NotifyPinnedItemsChanged(bool doUpdateQuickAccessWidget)
		{
			await App.QuickAccessManager.Model.LoadAsync();
			if (doUpdateQuickAccessWidget)
				App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, null!);
		}

		public async Task SaveAsync(string[] items)
		{
			if (Equals(items, App.QuickAccessManager.Model.PinnedFolders.ToArray()))
				return;

			if (App.QuickAccessManager.PinnedItemsWatcher is not null)
				App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = false;

			// Unpin every item that is below this index and then pin them all in order
			await UnpinFromSidebarAsync([], false);

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
