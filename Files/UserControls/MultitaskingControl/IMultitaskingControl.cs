using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;

namespace Files.UserControls.MultitaskingControl
{
    public interface IMultitaskingControl
    {
        public delegate void CurrentInstanceChangedEventHandler(object sender, CurrentInstanceChangedEventArgs e);

        public void UpdateSelectedTab(string tabHeader, string workingDirectoryPath, bool isSearchResultsPage);

        event CurrentInstanceChangedEventHandler CurrentInstanceChanged;

        ObservableCollection<TabItem> Items { get; }

        void MultitaskingControl_Loaded(object sender, RoutedEventArgs e);
    }

    public class CurrentInstanceChangedEventArgs : EventArgs
    {
        public IShellPage CurrentInstance { get; set; }
        public List<IShellPage> ShellPageInstances { get; set; }
    }
}