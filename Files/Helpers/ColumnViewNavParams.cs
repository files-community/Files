using Files.Filesystem;
using Files.Interacts;
using System.Collections.ObjectModel;

namespace Files.Helpers
{
    public class ColumnViewNavParams
    {
        public string Path { get; set; }
        public int BladeNumber { get; set; }
        public ReadOnlyObservableCollection<ListedItem> ItemsSource { get; set; }
        public ItemViewModel ViewModel { get; set; }
        public Interaction Interaction { get; set; }
        public IShellPage CurrentInstance { get; set; }
    }
}