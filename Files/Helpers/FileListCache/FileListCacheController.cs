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

        private readonly IMemoryCache fileNamesCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1_000_000
        });

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
                fileNamesCache.Remove(path);
                return persistentAdapter.SaveFileDisplayNameToCache(path, displayName);
            }
            fileNamesCache.Set(path, displayName, new MemoryCacheEntryOptions
            {
                Size = 1
            });

            // save entry to persistent cache in background
            return persistentAdapter.SaveFileDisplayNameToCache(path, displayName);
        }
    }
}