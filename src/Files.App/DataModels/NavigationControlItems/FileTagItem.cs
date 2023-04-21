using Files.Backend.ViewModels.FileTags;

namespace Files.App.DataModels.NavigationControlItems
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
				ToolTipText = Text;
			}
		}

		public string ToolTipText { get; private set; }

		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.FileTag;

		public int CompareTo(INavigationControlItem other)
			=> Text.CompareTo(other.Text);

		public TagViewModel FileTag { get; set; }
	}
}
