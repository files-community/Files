using Files.Common;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public class LocationItem : ObservableObject, INavigationControlItem
    {
        public SvgImageSource Icon { get; set; }

        public string Text { get; set; }

        private string path;

        public string Path
        {
            get => path;
            set
            {
                path = value;
                HoverDisplayText = Path.Contains("?") || Path.ToLower().StartsWith("shell:") || Path.ToLower().EndsWith(ShellLibraryItem.EXTENSION) || Path == "Home" ? Text : Path;
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
            get
            {
                if (Text.Equals("SidebarCloudDrives".GetLocalized()))
                    return App.AppSettings.Get(Text == "SidebarCloudDrives".GetLocalized(), $"section:{Text}");
                else if (Text.Equals("SidebarDrives".GetLocalized()))
                    return App.AppSettings.Get(Text == "SidebarDrives".GetLocalized(), $"section:{Text}");
                else if (Text.Equals("SidebarFavorites".GetLocalized()))
                    return App.AppSettings.Get(Text == "SidebarFavorites".GetLocalized(), $"section:{Text}");
                else if (Text.Equals("SidebarLibraries".GetLocalized()))
                    return App.AppSettings.Get(Text == "SidebarLibraries".GetLocalized(), $"section:{Text}");
                else if (Text.Equals("Network".GetLocalized()))
                    return App.AppSettings.Get(Text == "Network".GetLocalized(), $"section:{Text}");
                else if (Text.Equals("WSL"))
                    return App.AppSettings.Get(Text == "WSL", $"section:{Text}");
                else
                    return false;
            }
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