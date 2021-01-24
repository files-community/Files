namespace Files.Filesystem
{
    public interface INavigationControlItem
    {
        public string Glyph { get; }

        public string Text { get; }

        public string Path { get; }

        public string HoverDisplayText { get; }

        public NavigationControlItemType ItemType { get; }
    }

    public enum NavigationControlItemType
    {
        Header,
        Drive,
        LinuxDistro,
        Location,
        CloudDrive
    }
}