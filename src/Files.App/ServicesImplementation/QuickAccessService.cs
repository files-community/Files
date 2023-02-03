using Files.App.Shell;
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
		
		public async Task<List<string>> GetPinnedFoldersAsync()
		{
			var sidebarItems =  (await Win32Shell.GetShellFolderAsync(guid, "Enumerate", 0, 10000)).Enumerate
				.Where(link => link.IsFolder)
				.Select(link => link.FilePath).ToList();

			if (sidebarItems.Count > 4) // Avoid first opening crash #11139
				sidebarItems.RemoveRange(sidebarItems.Count - 4, 4); // 4 is the number of recent items shown in explorer sidebar
			
			return sidebarItems;
		}

		public async Task PinToSidebar(string folderPath)
			=> await PinToSidebar(new[] { folderPath });
		
		public async Task PinToSidebar(string[] folderPaths)
		{
			await ContextMenu.InvokeVerb("pintohome", folderPaths);
			await App.QuickAccessManager.Model.LoadAsync();
		}

		public async Task UnpinFromSidebar(string folderPath)
			=> await UnpinFromSidebar(new[] { folderPath });
		
		public async Task UnpinFromSidebar(string[] folderPaths)
		{
			Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
			object? shell = Activator.CreateInstance(shellAppType);
			dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { $"shell:{guid}" });

			foreach (dynamic? fi in f2.Items())
				if (folderPaths.Contains((string)fi.Path))
					await SafetyExtensions.IgnoreExceptions(async () => { 
						await fi.InvokeVerb("unpinfromhome");
					});

			await App.QuickAccessManager.Model.LoadAsync();
		}
	}
}
