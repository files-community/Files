using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Files.App.UserControls.MultitaskingControl
{
    public interface IMultitaskingControl
    {
        public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

        public ObservableCollection<TabItem> Items { get; }

        public IPaneContent GetCurrentSelectedTabInstance();

        public List<IPaneContent> GetAllTabInstances();

        public void CloseTab(TabItem tabItem);

        public void SetLoadingIndicatorStatus(TabItem item, bool loading);
    }

    public class CurrentInstanceChangedEventArgs : EventArgs
    {
        public IPaneContent CurrentInstance { get; set; }
        public List<IPaneContent> PageInstances { get; set; }
    }
}