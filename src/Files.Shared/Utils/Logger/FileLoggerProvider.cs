// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Files.Shared
{
	public sealed class FileLoggerProvider : ILoggerProvider
	{
		private readonly FileLogger logger;

		public FileLoggerProvider(string path)
		{
			logger = new FileLogger(path);
		}

		public ILogger CreateLogger(string categoryName)
			=> logger;

		public void Dispose()
		{
			logger.Dispose();
		}
	}
}
