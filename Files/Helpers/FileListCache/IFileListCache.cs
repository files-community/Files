using System.Threading.Tasks;

namespace Files.Helpers.FileListCache
{
    internal interface IFileListCache
    {
        public Task<CacheEntry> ReadFileListFromCache(string path);

        public Task SaveFileListToCache(string path, CacheEntry cacheEntry);
    }
}