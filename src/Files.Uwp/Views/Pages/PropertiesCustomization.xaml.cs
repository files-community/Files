﻿using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Files.Uwp.Views
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
