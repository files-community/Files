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

			App.Logger.Warn("Is a shortcut file");

			var isApplication = !string.IsNullOrWhiteSpace(shortcutItem.TargetPath) &&
			   FileExtensionHelpers.IsExecutableFile(shortcutItem.TargetPath) || 
			   FileExtensionHelpers.IsMsiFile(shortcutItem.TargetPath);

			await App.Window.DispatcherQueue.EnqueueAsync(() =>
				UIFilesystemHelpers.SetShortcutIsRunAsAdmin(shortcutItem, ViewModel.RunAsAdmin, AppInstance)
			);

			return true;
		}

		public override void Dispose()
		{
		}
	}
}