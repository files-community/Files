using Files.Filesystem;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;

namespace Files.Helpers
{
    internal class FileListCache
    {
        private static FileListCache instance;
        public static FileListCache GetInstance()
        {
            return instance ??= new FileListCache();
        }

        private readonly IMemoryCache filesCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 512
        });

        public void CacheFileList(string path, IList<ListedItem> fileList, ListedItem currentFolder)
        {
            if (!App.AppSettings.UseFileListCache) return;
            var entry = new CacheEntry
            {
                FileList = fileList.ToList(),
                CurrentFolder = currentFolder
            };
            filesCache.Set(path, entry, new MemoryCacheEntryOptions
            {
                Size = 1
            });
        }

        public (IList<ListedItem>, ListedItem) ReadFileListFromCache(string path)
        {
            if (!App.AppSettings.UseFileListCache) return (null, null);
            var entry = filesCache.Get<CacheEntry>(path);
            return (entry?.FileList, entry?.CurrentFolder);
        }

        private class CacheEntry
        {
            public List<ListedItem> FileList { get; set; }
            public ListedItem CurrentFolder { get; set; }
        }
    }
}
