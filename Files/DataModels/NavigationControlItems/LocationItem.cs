using Files.Common;
using Files.Filesystem;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.DataModels.NavigationControlItems
{
    public class LocationItem : ObservableObject, INavigationControlItem
    {
        public BitmapImage icon;

        public BitmapImage Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public Uri IconSource { get; set; }
        public byte[] IconData { get; set; }

        public string Text { get; set; }

        private string path;

        public string Path
        {
            get => path;
            set
            {
                path = value;
                HoverDisplayText = Path.Contains("?") || Path.ToLower().StartsWith("shell:") || Path.ToLower().EndsWith(ShellLibraryItem.EXTENSION) || Path == "Home".GetLocalized() ? Text : Path;
            }
        }

        public string HoverDisplayText { get; private set; }
        public FontFamily Font { get; set; } = new FontFamily("Segoe MDL2 Assets");
        public NavigationControlItemType ItemType => NavigationControlItemType.Location;
        public bool IsDefaultLocation { get; set; }
        public ObservableCollection<INavigationControlItem> ChildItems { get; set; }

        public bool SelectsOnInvoked { get; set; } = true;

        public bool IsExpanded
        {
            get => App.AppSettings.Get(Text == "SidebarFavorites".GetLocalized(), $"section:{Text}");
            set
            {
                App.AppSettings.Set(value, $"section:{Text}");
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        public SectionType Section { get; set; }

        public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);
    }
}