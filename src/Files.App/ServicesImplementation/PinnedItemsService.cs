using Files.App.Controllers;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Shell;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Files.App.ServicesImplementation
{
	internal class PinnedItemsService
	{
		private readonly static SidebarPinnedController Controller = App.SidebarPinnedController;
		public static async Task<List<string>> GetRecentFilesAsync()
		{
			return (await Win32Shell.GetShellFolderAsync("::{679f85cb-0220-4080-b29b-5540cc05aab6}", "Enumerate", 0, 10000)).Enumerate
				.Where(link => link.IsFolder)
				.Select(link => link.FilePath).ToList();
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
			{
				if (folderPaths.Contains((string)fi.Path))
				{
					App.Logger.Info($"Unpinning {fi.Verbs()}");
					await SafetyExtensions.IgnoreExceptions(async () => { 
						await fi.InvokeVerb("unpinfromhome");
					});
				}
			}

			await Controller.LoadAsync();
		}
	}
}
