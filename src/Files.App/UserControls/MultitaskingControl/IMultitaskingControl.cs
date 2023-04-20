using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Files.App.UserControls.MultitaskingControl
{
	public interface IMultitaskingControl
	{
		public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

		public ObservableCollection<TabItem> Items { get; }

		public ITabItemContent GetCurrentSelectedTabInstance();

		public List<ITabItemContent> GetAllTabInstances();

		public Task ReopenClosedTab();

		public void CloseTab(TabItem tabItem);

		public void SetLoadingIndicatorStatus(ITabItem item, bool loading);
	}

	public class CurrentInstanceChangedEventArgs : EventArgs
	{
		public ITabItemContent CurrentInstance { get; set; }
		public List<ITabItemContent> PageInstances { get; set; }
	}
}