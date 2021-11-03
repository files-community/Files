using Files.Filesystem;
using Files.ViewModels.Properties;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Files.Views
{
    public sealed partial class PropertiesCustomization : PropertiesTab
    {
        public PropertiesCustomization()
        {
            this.InitializeComponent();
        }

        private void CustomIconsSelectorFrame_Loaded(object sender, RoutedEventArgs e)
        {
            string initialPath = @"C:\Windows\System32\SHELL32.dll";
            var item = (BaseProperties as FileProperties)?.Item ?? (BaseProperties as FolderProperties)?.Item;
            (sender as Frame).Navigate(typeof(CustomFolderIcons), new IconSelectorInfo
            {
                AppInstance = AppInstance,
                InitialPath = initialPath,
                SelectedItem = item.ItemPath,
                IsShortcut = item.IsShortcutItem
            }, new SuppressNavigationTransitionInfo());
        }

        public override async Task<bool> SaveChangesAsync(ListedItem item)
        {
            return await Task.FromResult(true);
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
