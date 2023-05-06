// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;

namespace Files.Shared
{
	public class FileLoggerProvider : ILoggerProvider
	{
		private readonly string path;

		public FileLoggerProvider(string path)
		{
			this.path = path;
		}

		public ILogger CreateLogger(string categoryName)
		{
			return new FileLogger(path);
		}

		public void Dispose()
		{
		}
	}
}
