// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Files.App.Services.Thumbnails.Generators
{
	public sealed class ShellApiThumbnailGenerator : IThumbnailGenerator
	{
		private readonly ILogger _logger;

		public IEnumerable<string> SupportedTypes => ["*"];

		public ShellApiThumbnailGenerator(ILogger<ShellApiThumbnailGenerator> logger)
		{
			_logger = logger;
		}

		public async Task<byte[]?> GenerateAsync(
			string path,
			int size,
			bool isFolder,
			IconOptions options,
			CancellationToken ct)
		{
			return await STATask.Run(() =>
				Win32Helper.GetIcon(path, size, isFolder, options),
				_logger);
		}
	}
}
