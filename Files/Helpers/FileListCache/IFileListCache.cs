using System.Threading;
using System.Threading.Tasks;

namespace Files.Helpers.FileListCache
{
    internal interface IFileListCache
    {
        public Task<CacheEntry> ReadFileListFromCache(string path, CancellationToken cancellationToken);

        public Task SaveFileListToCache(string path, CacheEntry cacheEntry);
    }
}