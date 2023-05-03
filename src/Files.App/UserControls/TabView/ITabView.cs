// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.UserControls.TabView
{
	public interface ITabView
	{
		public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

		public ObservableCollection<TabItem> Items { get; }

		public ITabItemContent GetCurrentSelectedTabInstance();

		public List<ITabItemContent> GetAllTabInstances();

		public Task ReopenClosedTab();

		public void CloseTab(TabItem tabItem);

		public void SetLoadingIndicatorStatus(ITabItem item, bool loading);
	}

}
