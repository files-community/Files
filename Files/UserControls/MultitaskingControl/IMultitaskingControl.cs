using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Files.UserControls.MultitaskingControl
{
    public interface IMultitaskingControl
    {
        public delegate void CurrentInstanceChangedEventHandler(object sender, CurrentInstanceChangedEventArgs e);

        public void UpdateSelectedTab(string tabHeader, string workingDirectoryPath, bool isSearchResultsPage);

        event CurrentInstanceChangedEventHandler CurrentInstanceChanged;

        ObservableCollection<TabItem> Items { get; }

        List<ITabItem> RecentlyClosedTabs { get; }

        bool RestoredRecentlyClosedTab { get; set; }

        public TTab GetCurrentSelectedTabInstance<TTab>();

        public List<TTab> GetAllTabInstances<TTab>();

        public void RemoveTab(TabItem tabItem);
    }

    public class CurrentInstanceChangedEventArgs : EventArgs
    {
        public IShellPage CurrentInstance { get; set; }
        public List<IShellPage> ShellPageInstances { get; set; }
    }
}