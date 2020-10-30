using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarSymbols;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Files.UserControls.MultiTaskingControl
{
    public interface IMultitaskingControl
    {
        public delegate void CurrentInstanceChangedEventHandler(object sender, CurrentInstanceChangedEventArgs e);
        Task SetSelectedTabInfo(string text, string currentPathForTabIcon);

        void SelectionChanged();

        event CurrentInstanceChangedEventHandler CurrentInstanceChanged;

        ObservableCollection<TabItem> Items { get; }
    }

    public class CurrentInstanceChangedEventArgs : EventArgs
    {
        public IShellPage CurrentInstance { get; set; }
        public List<IShellPage> ShellPageInstances { get; set; }
    }
}