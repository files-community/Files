﻿using Files.App.Shell;
using Files.App.UserControls.Widgets;
using Files.Shared;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
{
	internal class QuickAccessService : IQuickAccessService
	{
		private readonly static string guid = "::{679f85cb-0220-4080-b29b-5540cc05aab6}";

		public async Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync()
		{
			return (await Win32Shell.GetShellFolderAsync(guid, "Enumerate", 0, int.MaxValue, "System.Home.IsPinned")).Enumerate
				.Where(link => link.IsFolder);
		}

		public Task PinToSidebar(string folderPath)
		{ 
			return PinToSidebar(new[] { folderPath });
		}
		
		public async Task PinToSidebar(string[] folderPaths)
		{
			foreach (string folderPath in folderPaths)
				await ContextMenu.InvokeVerb("pintohome", new[] {folderPath});

			await App.QuickAccessManager.Model.LoadAsync();
			
			App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, true));
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
				if (folderPaths.Contains((string)fi.Path)
					|| (string.Equals(fi.Path, "::{645FF040-5081-101B-9F08-00AA002F954E}") && folderPaths.Contains(Constants.CommonPaths.RecycleBinPath)))
				{
					await SafetyExtensions.IgnoreExceptions(async () =>
					{
						await fi.InvokeVerb("unpinfromhome");
					});
				}
			}

			await App.QuickAccessManager.Model.LoadAsync();
			
			App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, false));
		}

		public bool IsItemPinned(string folderPath)
		{
			return App.QuickAccessManager.Model.FavoriteItems.Contains(folderPath);
		}

		public async Task Save(string[] items)
		{
			if (Equals(items, App.QuickAccessManager.Model.FavoriteItems.ToArray()))
				return;

			App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = false;

			// Unpin every item that is below this index and then pin them all in order
			await UnpinFromSidebar(Array.Empty<string>());

			await PinToSidebar(items);
			App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = true;
			await App.QuickAccessManager.Model.LoadAsync();
		}
	}
}
