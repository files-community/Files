using System.Threading;
using System.Threading.Tasks;

namespace Files.Helpers.FileListCache
{
    internal interface IFileListCache
    {
        public Task<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken);

        public Task SaveFileDisplayNameToCache(string path, string displayName);
    }
}