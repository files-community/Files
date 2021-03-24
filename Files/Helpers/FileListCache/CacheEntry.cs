using Files.Filesystem;
using System.Collections.Generic;

namespace Files.Helpers.FileListCache
{
    internal class CacheEntry
    {
        public List<ListedItem> FileList { get; set; }
        public ListedItem CurrentFolder { get; set; }
    }
}