using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Shell;
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
		public static async Task<List<string>> GetRecentFilesAsync()
		{
			return (await Win32Shell.GetShellFolderAsync("::{679f85cb-0220-4080-b29b-5540cc05aab6}", "Enumerate", 0, 10000)).Enumerate
				.Where(link => link.IsFolder)
				.Select(link => link.FilePath).ToList();
		}

		public static Task PinToSidebar(string[] folderPaths)
			=> ContextMenu.InvokeVerb("pintohome", folderPaths);

		public static void UnpinFromSidebar(string[] folderPaths)
		{
			Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
			object? shell = Activator.CreateInstance(shellAppType);
			dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { "shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}" });

			foreach (dynamic? fi in f2.Items())
			{
				if (folderPaths.Contains((string)fi.Path))
				{
					fi.InvokeVerb("unpinfromhome");
				}
			}
		}
	}
}
