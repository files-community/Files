// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services
{
	internal sealed class QuickAccessService : IQuickAccessService
	{
		public async Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync()
		{
			var result =
				(await Win32Helper.GetShellFolderAsync(Constants.CLID.QuickAccess, false, true, 0, int.MaxValue, "System.Home.IsPinned"))
					.Enumerate
					.Where(link => link.IsFolder);

			return result;
		}

		public async Task PinToSidebarAsync(string[] folderPaths, bool doUpdateQuickAccessWidget = true)
		{
			foreach (string folderPath in folderPaths)
				await ContextMenu.InvokeVerb("pintohome", [folderPath]);

			await App.QuickAccessManager.Model.LoadAsync();

			if (doUpdateQuickAccessWidget)
				App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, true));
		}

		public async Task UnpinFromSidebarAsync(string[] folderPaths, bool doUpdateQuickAccessWidget = true)
		{
			Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
			object? shell = Activator.CreateInstance(shellAppType);
			dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, [$"shell:{Constants.CLID.QuickAccess}"]);

			if (folderPaths.Length == 0)
				folderPaths = (await GetPinnedFoldersAsync())
					.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
					.Select(link => link.FilePath).ToArray();

			foreach (dynamic? fi in f2.Items())
			{
				if (ShellStorageFolder.IsShellPath((string)fi.Path))
				{
					var folder = await ShellStorageFolder.FromPathAsync((string)fi.Path);
					var path = folder?.Path;

					if (path is not null && 
						(folderPaths.Contains(path) || (path.StartsWith(@"\\SHELL\") && folderPaths.Any(x => x.StartsWith(@"\\SHELL\"))))) // Fix for the Linux header
					{
						await SafetyExtensions.IgnoreExceptions(async () =>
						{
							await fi.InvokeVerb("unpinfromhome");
						});
						continue;
					}
				}

				if (folderPaths.Contains((string)fi.Path))
				{
					await SafetyExtensions.IgnoreExceptions(async () =>
					{
						await fi.InvokeVerb("unpinfromhome");
					});
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

		public async Task SaveAsync(string[] items)
		{
			if (Equals(items, App.QuickAccessManager.Model.PinnedFolders.ToArray()))
				return;

			App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = false;

			// Unpin every item that is below this index and then pin them all in order
			await UnpinFromSidebarAsync([], false);

			await PinToSidebarAsync(items, false);
			App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = true;

			App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(items, true)
			{
				Reorder = true
			});
		}
	}
}
