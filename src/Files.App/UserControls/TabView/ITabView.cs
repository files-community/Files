// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.UserControls.TabView
{
	public interface ITabView
	{
		public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

		public ObservableCollection<TabViewItem> Items { get; }

		public ITabViewItemContent GetCurrentSelectedTabInstance();

		public List<ITabViewItemContent> GetAllTabInstances();

		public Task ReopenClosedTab();

		public void CloseTab(TabViewItem tabItem);

		public void SetLoadingIndicatorStatus(ITabViewItem item, bool loading);
	}
}
