using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Files.UserControls.MultiTaskingControl
{
    public interface IMultitaskingControl
    {
        Task SetSelectedTabInfo(string text, string currentPathForTabIcon);

        void SelectionChanged();

        ObservableCollection<TabItem> Items { get; }
    }
}