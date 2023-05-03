using Files.App.UserControls.TabView;

namespace Files.App.Data.EventArguments
{
	public class CurrentInstanceChangedEventArgs : EventArgs
	{
		public ITabItemContent CurrentInstance { get; set; }

		public List<ITabItemContent> PageInstances { get; set; }
	}
}
