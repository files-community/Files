using Files.Filesystem;
using System;

namespace Files.DataModels.NavigationControlItems
{
    public class WslDistroItem : INavigationControlItem
    {
        public string Text { get; set; }

        private string path;

        public string Path
        {
            get => path;
            set
            {
                path = value;
                HoverDisplayText = Path.Contains("?") ? Text : Path;
            }
        }

        public string HoverDisplayText { get; private set; }

        public NavigationControlItemType ItemType => NavigationControlItemType.LinuxDistro;

        public Uri Logo { get; set; }

        public SectionType Section { get; private set; }

        public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);
    }
}