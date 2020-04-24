using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem
{
    public interface INavigationControlItem
    {
        public string IconGlyph { get; }

        public string Text { get; }

        public string Path { get; }
        public NavigationControlItemType ItemType { get; }
    }

    public enum NavigationControlItemType
    {
        Header,
        Drive,
        LinuxDistro,
        Location,
        OneDrive
    }
}