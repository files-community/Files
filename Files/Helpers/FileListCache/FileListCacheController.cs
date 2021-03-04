using Microsoft.Extensions.Caching.Memory;
using System.Threading;
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

        private readonly IMemoryCache fileNamesCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1_000_000
        });

        public Task SaveFileListToCache(string path, CacheEntry cacheEntry)
        {
            if (!App.AppSettings.UseFileListCache)
            {
                return Task.CompletedTask;
            }

            if (cacheEntry == null)
            {
                filesCache.Remove(path);
                return persistentAdapter.SaveFileListToCache(path, cacheEntry);
            }
            filesCache.Set(path, cacheEntry, new MemoryCacheEntryOptions
            {
                Size = cacheEntry.FileList.Count
            });

            // save entry to persistent cache in background
            return persistentAdapter.SaveFileListToCache(path, cacheEntry);
        }

        public async Task<CacheEntry> ReadFileListFromCache(string path, CancellationToken cancellationToken)
        {
            if (!App.AppSettings.UseFileListCache)
            {
                return null;
            }

            var entry = filesCache.Get<CacheEntry>(path);
            if (entry == null)
            {
                entry = await persistentAdapter.ReadFileListFromCache(path, cancellationToken);
                if (entry?.FileList != null)
                {
                    filesCache.Set(path, entry, new MemoryCacheEntryOptions
                    {
                        Size = entry.FileList.Count
                    });
                }
            }
            return entry;
        }

        public async Task<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken)
        {
            var displayName = fileNamesCache.Get<string>(path);
            if (displayName == null)
            {
                displayName = await persistentAdapter.ReadFileDisplayNameFromCache(path, cancellationToken);
                if (displayName != null)
                {
                    fileNamesCache.Set(path, displayName, new MemoryCacheEntryOptions
                    {
                        Size = 1
                    });
                }
            }
            return displayName;
        }

        public Task SaveFileDisplayNameToCache(string path, string displayName)
        {
            if (displayName == null)
            {
                filesCache.Remove(path);
                return persistentAdapter.SaveFileDisplayNameToCache(path, displayName);
            }
            filesCache.Set(path, displayName, new MemoryCacheEntryOptions
            {
                Size = 1
            });

            // save entry to persistent cache in background
            return persistentAdapter.SaveFileDisplayNameToCache(path, displayName);
        }
    }
}