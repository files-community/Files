using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;

namespace Files.Filesystem
{
    public class LocationItem : ObservableObject, INavigationControlItem
    {
        public string Glyph { get; set; }
        public string Text { get; set; }

        private string path;

        public string Path
        {
            get => path;
            set
            {
                path = value;
                HoverDisplayText = Path.Contains("?") || Path.StartsWith("Shell:") || Path == "Home" ? Text : Path;
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
                if (Text.Equals("SidebarFavorites".GetLocalized()))
                    return App.AppSettings.ShowSidebarFavoritesExpanded;
                else if (Text.Equals("SidebarDrives".GetLocalized()))
                    return App.AppSettings.ShowSidebarDrivesExpanded;
                else if (Text.Equals("SidebarCloudDrives".GetLocalized()))
                    return App.AppSettings.ShowSidebarCloudDrivesExpanded;
                else if (Text.Equals("SidebarNetworkDrives".GetLocalized()))
                    return App.AppSettings.ShowSidebarNetworkExpanded;
                else
                    return false;
            }
            set
            {
                if (Text.Equals("SidebarFavorites".GetLocalized()))
                    App.AppSettings.ShowSidebarFavoritesExpanded = value;
                else if (Text.Equals("SidebarDrives".GetLocalized()))
                    App.AppSettings.ShowSidebarDrivesExpanded = value;
                else if (Text.Equals("SidebarCloudDrives".GetLocalized()))
                    App.AppSettings.ShowSidebarCloudDrivesExpanded = value;
                else if (Text.Equals("SidebarNetworkDrives".GetLocalized()))
                    App.AppSettings.ShowSidebarNetworkExpanded = value;

                App.AppSettings.Set(value, $"section:{Text}");
                OnPropertyChanged(nameof(IsExpanded));
            }
        }
    }
}