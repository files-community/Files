using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem
{
    public class LocationItem : INavigationControlItem
    {
        public string Glyph { get; set; }
        public string Text { get; set; }
        public string Path { get; set; }
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