using Files.Filesystem;
using System.Collections.Generic;

namespace Files.Helpers.FileListCache
{
    internal class CacheEntry
    {
        public ListedItem CurrentFolder { get; set; }
        public List<ListedItem> FileList { get; set; }
    }
}