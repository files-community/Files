using CommunityToolkit.WinUI;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class ShortcutPage : BasePropertiesPage
	{
		public ShortcutPage()
		{
			InitializeComponent();
		}

		public override async Task<bool> SaveChangesAsync()
		{
			ShortcutItem? shortcutItem = BaseProperties switch
			{
				FileProperties properties => properties.Item as ShortcutItem,
				FolderProperties properties => properties.Item as ShortcutItem,
				_ => null
			};

			if (shortcutItem is null)
				return true;

			await App.Window.DispatcherQueue.EnqueueAsync(() =>
				UIFilesystemHelpers.UpdateShortcutItemProperties(
					shortcutItem,
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
