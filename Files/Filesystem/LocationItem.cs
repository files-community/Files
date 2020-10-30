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

    public class HeaderTextItem : INavigationControlItem
    {
        public string Glyph { get; set; } = null;
        public string Text { get; set; }
        public string Path { get; set; } = null;
        public NavigationControlItemType ItemType => NavigationControlItemType.Header;
    }
}