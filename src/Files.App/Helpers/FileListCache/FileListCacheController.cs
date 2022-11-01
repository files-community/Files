using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Helpers.FileListCache
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

		public ValueTask<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken)
		{
			if (fileNamesCache.TryGetValue(path, out var displayName))
			{
				return ValueTask.FromResult(displayName);
			}

			return ValueTask.FromResult<string>(null);
		}

		public ValueTask SaveFileDisplayNameToCache(string path, string displayName)
		{
			if (displayName is null)
			{
				fileNamesCache.TryRemove(path, out _);
			}

			fileNamesCache[path] = displayName;
			return ValueTask.CompletedTask;
		}
	}
}