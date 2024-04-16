using Files.Backend.ViewModels.FileTags;
using Files.Uwp.Filesystem;

namespace Files.Uwp.DataModels.NavigationControlItems
{
    public class FileTagItem : INavigationControlItem
    {
        public string Text { get; set; }

        private string path;

        public string Path
        {
            get => path;
            set
            {
                path = value;
                HoverDisplayText = Text;
            }
        }

        public string HoverDisplayText { get; private set; }

        public SectionType Section { get; set; }

        public ContextMenuOptions MenuOptions { get; set; }

        public NavigationControlItemType ItemType => NavigationControlItemType.FileTag;

        public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);

        public FileTagViewModel FileTag { get; set; }
    }
}
