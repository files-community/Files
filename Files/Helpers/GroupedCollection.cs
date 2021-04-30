using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public class GroupedCollection<T> : ObservableCollection<T>, ISectionHeader
    {
        public string Key { get; set; }


        public GroupedCollection(IEnumerable<T> items) : base(items)
        {
        }
    }

    // This is needed because xaml data types can't be generic
    public class GroupedItemCollection : GroupedCollection<ListedItem>
    {
        public GroupedItemCollection(IEnumerable<ListedItem> items) : base(items)
        {
        }
    }

    public interface ISectionHeader
    {
        public string Key { get; set; }
    }
}
