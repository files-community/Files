using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;

namespace Files.Helpers.FileListCache
{
    internal class FileListCacheController : IFileListCache
    {
        private static FileListCacheController instance;

        public static FileListCacheController GetInstance()
        {
            return instance ??= new FileListCacheController();
        }

        private readonly IFileListCache persistentAdapter;

        private FileListCacheController()
        {
            persistentAdapter = new PersistentSQLiteCacheAdapter();
        }

        private readonly IMemoryCache filesCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1_000_000
        });

        public Task SaveFileListToCache(string path, CacheEntry cacheEntry)
        {
            if (!App.AppSettings.UseFileListCache) return Task.CompletedTask;
            filesCache.Set(path, cacheEntry, new MemoryCacheEntryOptions
            {
                Size = cacheEntry.FileList.Count
            });

            // save entry to persistent cache in background
            return Task.Run(() => persistentAdapter.SaveFileListToCache(path, cacheEntry));
        }

        public async Task<CacheEntry> ReadFileListFromCache(string path)
        {
            if (!App.AppSettings.UseFileListCache) return null;
            var entry = filesCache.Get<CacheEntry>(path);
            return entry ?? await Task.Run(() => persistentAdapter.ReadFileListFromCache(path));
        }
    }
}