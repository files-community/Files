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
    public class GroupedCollection<T> : BulkConcurrentObservableCollection<T>, IGroupedCollectionHeader
    {
        public string Key { get; set; }


        public GroupedCollection(IEnumerable<T> items) : base(items)
        {
        }
        public GroupedCollection() : base()
        {
        }
    }

    public interface IGroupedCollectionHeader
    {
        public string Key { get; set; }
    }
}
