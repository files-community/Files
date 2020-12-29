using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Files.UserControls.MultitaskingControl
{
    public interface IMultitaskingControl
    {
        public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

        public ObservableCollection<TabItem> Items { get; }

        public List<ITabItem> RecentlyClosedTabs { get; }

        public ITabItemContent GetCurrentSelectedTabInstance();

        public List<ITabItemContent> GetAllTabInstances();

        public void RemoveTab(TabItem tabItem);
    }

    public class CurrentInstanceChangedEventArgs : EventArgs
    {
        public ITabItemContent CurrentInstance { get; set; }
        public List<ITabItemContent> PageInstances { get; set; }
    }
}