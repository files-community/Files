using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Views
{
	public sealed partial class PropertiesCustomization : PropertiesTab
	{
		public PropertiesCustomization()
		{
			this.InitializeComponent();
		}

		private void CustomIconsSelectorFrame_Loaded(object sender, RoutedEventArgs e)
		{
			string initialPath = Path.Combine(CommonPaths.SystemRootPath, "System32", "SHELL32.dll");
			var item = (BaseProperties as FileProperties)?.Item ?? (BaseProperties as FolderProperties)?.Item;
			(sender as Frame).Navigate(typeof(CustomFolderIcons), new IconSelectorInfo
			{
				AppInstance = AppInstance,
				InitialPath = initialPath,
				SelectedItem = item.ItemPath,
				IsShortcut = item.IsShortcut
			}, new SuppressNavigationTransitionInfo());
		}

		public override Task<bool> SaveChangesAsync(ListedItem item)
		{
			return Task.FromResult(true);
		}

		public override void Dispose()
		{
		}

		public class IconSelectorInfo
		{
			public IShellPage AppInstance;
			public string SelectedItem;
			public bool IsShortcut;
			public string InitialPath;
		}
	}
}
