using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Files.UserControls.MultitaskingControl
{
    public interface IMultitaskingControl
    {
        public ITabItemContent CurrentSelectedAppInstance { get; }

        public TabView TabViewControl { get; }

        public TabItem SelectedTabItem { get; }

        public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

        public ObservableCollection<TabItem> Items { get; }

        public ITabItemContent GetCurrentSelectedTabInstance();

        public List<ITabItemContent> GetAllTabInstances();

        public void CloseTab(TabItem tabItem);

        public void SetLoadingIndicatorStatus(ITabItem item, bool loading);

        public Task AddNewTabByPathAsync(Type type, string path, int atIndex = -1);

        public Task AddNewTabByParam(Type type, string tabViewItemArgs, int atIndex = -1);

        public void AddNewTab();
    }

    public class CurrentInstanceChangedEventArgs : EventArgs
    {
        public ITabItemContent CurrentInstance { get; set; }
        public List<ITabItemContent> PageInstances { get; set; }
    }
}