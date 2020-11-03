using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarSymbols;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Files.UserControls.MultiTaskingControl
{
    public interface IMultitaskingControl
    {
        public delegate void CurrentInstanceChangedEventHandler(object sender, CurrentInstanceChangedEventArgs e);
        public void UpdateSelectedTab(string tabHeader, string workingDirectoryPath);

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