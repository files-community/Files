using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.UserControls.MultiTaskingControl
{
    public interface IMultitaskingControl
    {
        void SetSelectedTabInfo(string text, string currentPathForTabIcon);
        void SelectionChanged();
        ObservableCollection<TabItem> Items { get; }
    }
}
