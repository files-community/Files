using Files.App.Shell;
using Files.App.UserControls.Widgets;
using Files.Sdk.Storage.LocatableStorage;
using Files.Core;
using Files.Core.Extensions;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
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
			=> PinToSidebar(new[] { folderPath });
		
		public async Task PinToSidebar(string[] folderPaths)
		{
			await ContextMenu.InvokeVerb("pintohome", folderPaths);

			await App.QuickAccessManager.Model.LoadAsync();
			
			App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, true));
		}
		
		public Task UnpinFromSidebar(string folderPath)
			=> UnpinFromSidebar(new[] { folderPath });
		
		public async Task UnpinFromSidebar(string[] folderPaths)
		{
			Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
			object? shell = Activator.CreateInstance(shellAppType);
			dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { $"shell:{guid}" });

			foreach (dynamic? fi in f2.Items())
				if (folderPaths.Contains((string)fi.Path)
					|| (string.Equals(fi.Path, "::{645FF040-5081-101B-9F08-00AA002F954E}") && folderPaths.Contains(Constants.CommonPaths.RecycleBinPath)))
					await SafetyExtensions.IgnoreExceptions(async () => {
						await fi.InvokeVerb("unpinfromhome");
					});

			await App.QuickAccessManager.Model.LoadAsync();
			
			App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, false));
		}

		public bool IsItemPinned(string folderPath)
		{
			return App.QuickAccessManager.Model.FavoriteItems.Contains(folderPath);
		}
	}
}
