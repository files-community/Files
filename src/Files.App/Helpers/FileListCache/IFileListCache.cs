namespace Files.App.Helpers.FileListCache
{
	internal interface IFileListCache
	{
		public ValueTask<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken);

		public ValueTask SaveFileDisplayNameToCache(string path, string displayName);
	}
}