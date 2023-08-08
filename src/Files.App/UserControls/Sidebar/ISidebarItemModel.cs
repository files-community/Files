using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.Sidebar
{
	public interface ISidebarItemModel : INotifyPropertyChanged
	{
		public string Text { get; }

		public BulkConcurrentObservableCollection<INavigationControlItem>? ChildItems { get; }

		public IconSource? IconSource { get; }

		public object ToolTip { get; }
		public bool IsExpanded { get; set; }
	}
}
