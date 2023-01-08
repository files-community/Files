using CommunityToolkit.WinUI;
using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Files.App.Helpers;
using System;
using System.Threading.Tasks;

namespace Files.App.Views
{
	public sealed partial class PropertiesShortcut : PropertiesTab
	{
		public PropertiesShortcut()
		{
			InitializeComponent();
		}

		public override async Task<bool> SaveChangesAsync()
		{
			var shortcutItem = BaseProperties switch
			{
				FileProperties properties => properties.Item,
				FolderProperties properties => properties.Item,
				_ => null
			} as ShortcutItem;

			if (shortcutItem is null)
				return true;

			await App.Window.DispatcherQueue.EnqueueAsync(() =>
				UIFilesystemHelpers.UpdateShortcutItemProperties(shortcutItem, 
				ViewModel.ShortcutItemPath,
				ViewModel.ShortcutItemArguments, 
				ViewModel.ShortcutItemWorkingDir, 
				ViewModel.RunAsAdmin)
			);

			return true;
		}

		public override void Dispose()
		{
		}
	}
}