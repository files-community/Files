using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Shell;
using Files.App.UserControls.Widgets;
using Files.Shared;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

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
			var quickAccessItems = (await Win32Shell.GetShellFolderAsync(guid, "Enumerate", 0, int.MaxValue)).Enumerate;

			foreach (var itemPath in folderPaths)
			{
				var item = quickAccessItems.FirstOrDefault(x => x.FilePath == itemPath);
				if (item is null)
					continue;

				await SafetyExtensions.IgnoreExceptions(async () =>
				{
					using var pidl = new Shell32.PIDL(item.PIDL);
					using var shellItem = ShellItem.Open(pidl);
					using var cMenu = await ContextMenu.GetContextMenuForFiles(new[] { shellItem }, Shell32.CMF.CMF_NORMAL);
					if (cMenu is not null)
						await cMenu.InvokeVerb("unpinfromhome");
				});
			}

			await App.QuickAccessManager.Model.LoadAsync();
			
			App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, false));
		}
	}
}
