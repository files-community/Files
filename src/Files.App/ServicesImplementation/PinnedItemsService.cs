using Files.App.Controllers;
using Files.App.Shell;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
{
	internal class PinnedItemsService
	{
		private readonly static SidebarPinnedController Controller = App.SidebarPinnedController;
		private readonly static string guid = "::{679f85cb-0220-4080-b29b-5540cc05aab6}";
		public static async Task<List<string>> GetPinnedFilesAsync()
		{
			var sidebarItems =  (await Win32Shell.GetShellFolderAsync(guid, "Enumerate", 0, 10000)).Enumerate
				.Where(link => link.IsFolder)
				.Select(link => link.FilePath).ToList();

			sidebarItems.RemoveRange(sidebarItems.Count - 4, 4); // 4 is the number of recent items shown in explorer sidebar
			return sidebarItems;
		}

		public static async Task PinToSidebar(string folderPath)
			=> await PinToSidebar(new[] { folderPath }, true);
		public static async Task PinToSidebar(string folderPath, bool loadExplorerItems)
			=> await PinToSidebar(new[] { folderPath }, loadExplorerItems);
		public static async Task PinToSidebar(string[] folderPaths)
			=> await PinToSidebar(folderPaths, true);
		public static async Task PinToSidebar(string[] folderPaths, bool loadExplorerItems)
		{
			await ContextMenu.InvokeVerb("pintohome", folderPaths);
			if (loadExplorerItems)
				await Controller.LoadAsync();
		}

		public static async Task UnpinFromSidebar(string folderPath)
			=> await UnpinFromSidebar(new[] { folderPath }, true);
		public static async Task UnpinFromSidebar(string folderPath, bool loadExplorerItems)
			=> await UnpinFromSidebar(new[] { folderPath }, loadExplorerItems);
		public static async Task UnpinFromSidebar(string[] folderPaths)
			=> await UnpinFromSidebar(folderPaths, true);
		public static async Task UnpinFromSidebar(string[] folderPaths, bool loadExplorerItems)
		{
			Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
			object? shell = Activator.CreateInstance(shellAppType);
			dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { $"shell:{guid}" });

			foreach (dynamic? fi in f2.Items())
				if (folderPaths.Contains((string)fi.Path))
					await SafetyExtensions.IgnoreExceptions(async () => { 
						await fi.InvokeVerb("unpinfromhome");
					});

			if (loadExplorerItems)
				await Controller.LoadAsync();
		}

		public static async Task Save(string[] toRemove)
		{
			// Saves pinned items by unpinning the previous items from explorer and then pinning the current items back
			await UnpinFromSidebar(toRemove, false);
			await PinToSidebar(Controller.Model.FavoriteItems.ToArray(), false);
		}
	}
}
