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

		public override async Task<bool> SaveChangesAsync(ListedItem item)
		{
			var shortcutItem = (ShortcutItem)item;

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