using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem
{
    public class LocationItem : INavigationControlItem
    {
        public bool IsDefaultLocation { get; set; }
        public string Glyph { get; set; }
        public string Text { get; set; }
        public string Path { get; set; }

        string INavigationControlItem.IconGlyph => Glyph;

        string INavigationControlItem.Text => Text;

        string INavigationControlItem.Path => Path;

        NavigationControlItemType INavigationControlItem.ItemType => NavigationControlItemType.Location;
    }

    public class HeaderTextItem : INavigationControlItem
    {
        string INavigationControlItem.IconGlyph => throw new NotImplementedException();

        string INavigationControlItem.Text => throw new NotImplementedException();

        string INavigationControlItem.Path => throw new NotImplementedException();

        NavigationControlItemType INavigationControlItem.ItemType => NavigationControlItemType.Header;
    }
}
