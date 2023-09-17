// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Shell;
using Files.App.UserControls.Widgets;

namespace Files.App.Services
{
	internal class QuickAccessService : IQuickAccessService
	{
		private static readonly QuickAccessManager _quickAccessManager = Ioc.Default.GetRequiredService<QuickAccessManager>();

		private readonly static string guid = "::{679f85cb-0220-4080-b29b-5540cc05aab6}";

		public async Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync()
		{
			var result = (await Win32Shell.GetShellFolderAsync(guid, "Enumerate", 0, int.MaxValue, "System.Home.IsPinned")).Enumerate
				.Where(link => link.IsFolder);
			return result;
		}

		public Task PinToSidebar(string folderPath)
		{ 
			return PinToSidebar(new[] { folderPath });
		}
		
		public async Task PinToSidebar(string[] folderPaths)
		{
			foreach (string folderPath in folderPaths)
				await ContextMenu.InvokeVerb("pintohome", new[] {folderPath});

			await _quickAccessManager.Model.LoadAsync();
			
			_quickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, true));
		}

		public Task UnpinFromSidebar(string folderPath)
		{ 
			return UnpinFromSidebar(new[] { folderPath }); 
		}
		
		public async Task UnpinFromSidebar(string[] folderPaths)
		{
			Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
			object? shell = Activator.CreateInstance(shellAppType);
			dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { $"shell:{guid}" });

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

			await _quickAccessManager.Model.LoadAsync();
			
			_quickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, false));
		}

		public bool IsItemPinned(string folderPath)
		{
			return _quickAccessManager.Model.FavoriteItems.Contains(folderPath);
		}

		public async Task Save(string[] items)
		{
			if (Equals(items, _quickAccessManager.Model.FavoriteItems.ToArray()))
				return;

			_quickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = false;

			// Unpin every item that is below this index and then pin them all in order
			await UnpinFromSidebar(Array.Empty<string>());

			await PinToSidebar(items);
			_quickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = true;
			await _quickAccessManager.Model.LoadAsync();
		}
	}
}
