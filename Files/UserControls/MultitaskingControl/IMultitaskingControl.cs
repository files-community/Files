using System.Collections.ObjectModel;

namespace Files.UserControls.MultiTaskingControl
{
    public interface IMultitaskingControl
    {
        void SetSelectedTabInfo(string text, string currentPathForTabIcon);

        void SelectionChanged();

        ObservableCollection<TabItem> Items { get; }
    }
}