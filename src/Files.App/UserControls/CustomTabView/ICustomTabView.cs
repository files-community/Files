// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.UserControls.CustomTabView
{
	/// <summary>
	/// Represents an interface for <see cref="UserControls.CustomTabView"/>.
	/// </summary>
	public interface ICustomTabView
	{
		public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

		public ObservableCollection<CustomTabViewItem> Items { get; }

		public ICustomTabViewItemContent GetCurrentSelectedTabInstance();

		public List<ICustomTabViewItemContent> GetAllTabInstances();

		public Task ReopenClosedTab();

		public void CloseTab(CustomTabViewItem tabItem);

		public void SetLoadingIndicatorStatus(ICustomTabViewItem item, bool loading);
	}
}
