using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;

namespace Files.Filesystem
{
    public class LocationItem : INavigationControlItem
    {
        public string Glyph { get; set; }
        public string Text { get; set; }
        public string Path { get; set; }
        public FontFamily Font { get; set; } = new FontFamily("Segoe MDL2 Assets");
        public NavigationControlItemType ItemType => NavigationControlItemType.Location;
        public bool IsDefaultLocation { get; set; }
    }

    public class HeaderItem : LocationItem
    {
        public enum HeaderItemType
        {
            ThisDevice,
            Drives,
            Cloud,
            Network
        }
        public bool IsItemExpanded { get; set; } = false;
        public HeaderItemType HeaderType { get; set; }
        public ObservableCollection<INavigationControlItem> MenuItems { get; set; } = new ObservableCollection<INavigationControlItem>();
        public new NavigationControlItemType ItemType => NavigationControlItemType.Header;
    }
}