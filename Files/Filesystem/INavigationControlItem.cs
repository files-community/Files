using System;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public enum NavigationControlItemType
    {
        Header,
        Drive,
        LinuxDistro,
        Location,
        CloudDrive
    }

    public enum SectionType
    {
        Home,
        Favorites,
        Library,
        Drives,
        CloudDrives,
        Network,
        WSL
    }

    public interface INavigationControlItem : IComparable<INavigationControlItem>
    {
        public string HoverDisplayText { get; }
        public SvgImageSource Icon { get; }

        public NavigationControlItemType ItemType { get; }
        public string Path { get; }
        public SectionType Section { get; }
        public string Text { get; }
    }
}