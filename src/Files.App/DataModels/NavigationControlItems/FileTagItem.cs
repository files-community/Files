using Files.Backend.ViewModels.FileTags;
using Files.App.Filesystem;

namespace Files.App.DataModels.NavigationControlItems
{
    public class FileTagItem : INavigationControlItem
    {
        public string Text { get; set; }

<<<<<<< HEAD
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
=======
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
>>>>>>> parent of 568a443d (Code cleanup)

        public string HoverDisplayText { get; private set; }

        public SectionType Section { get; set; }

        public ContextMenuOptions MenuOptions { get; set; }

        public NavigationControlItemType ItemType => NavigationControlItemType.FileTag;

        public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);

        public FileTagViewModel FileTag { get; set; }
    }
}
