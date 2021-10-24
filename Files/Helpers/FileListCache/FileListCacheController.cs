using System.Collections.Concurrent;
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

        private FileListCacheController()
        {
        }

        private readonly ConcurrentDictionary<string, string> fileNamesCache = new ConcurrentDictionary<string, string>();

        public Task<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken)
        {
            if (fileNamesCache.TryGetValue(path, out var displayName))
            {
                return Task.FromResult(displayName);
            }

            return Task.FromResult<string>(null);
        }

        public Task SaveFileDisplayNameToCache(string path, string displayName)
        {
            if (displayName == null)
            {
                fileNamesCache.TryRemove(path, out _);
            }

            fileNamesCache[path] = displayName;
            return Task.CompletedTask;
        }
    }
}