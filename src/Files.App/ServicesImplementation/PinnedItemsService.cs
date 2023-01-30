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
		public static async Task<List<string>> GetPinnedFilesAsync()
		{
			var sidebarItems =  (await Win32Shell.GetShellFolderAsync("::{679f85cb-0220-4080-b29b-5540cc05aab6}", "Enumerate", 0, 10000)).Enumerate
				.Where(link => link.IsFolder)
				.Select(link => link.FilePath).ToList();

			sidebarItems.RemoveRange(sidebarItems.Count - 4, 4); // 4 is the number of recent items shown in explorer sidebar
			return sidebarItems;
		}

		public static async Task PinToSidebar(string folderPath)
			=> await PinToSidebar(new[] { folderPath });
		public static async Task PinToSidebar(string[] folderPaths)
		{
			await ContextMenu.InvokeVerb("pintohome", folderPaths);
			await Controller.LoadAsync();
		}

		public static async Task UnpinFromSidebar(string folderPath)
			=> await UnpinFromSidebar(new[] { folderPath });
		public static async Task UnpinFromSidebar(string[] folderPaths)
		{
			Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
			object? shell = Activator.CreateInstance(shellAppType);
			dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { "shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}" });

			foreach (dynamic? fi in f2.Items())
				if (folderPaths.Contains((string)fi.Path))
					await SafetyExtensions.IgnoreExceptions(async () => { 
						await fi.InvokeVerb("unpinfromhome");
					});

			await Controller.LoadAsync();
		}

		// TODO: Fix
		public static async Task SetPinnedItemsAsync(IEnumerable<string> pinnedItems, IEnumerable<string> toClear)
		{
			await UnpinFromSidebar(toClear.ToArray());
			await PinToSidebar(pinnedItems.ToArray());
		}
	}
}
