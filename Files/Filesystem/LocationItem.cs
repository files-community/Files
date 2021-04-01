using Files.Common;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
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