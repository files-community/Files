using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.App.UserControls.Widgets
{
	public abstract class WidgetCardItem : ObservableObject
	{
		public string Path;

		public virtual object Item { get; set; }
	}
}
