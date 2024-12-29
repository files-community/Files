// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Files.App.Utils.Logger
{
	public sealed class SentryLoggerProvider : ILoggerProvider
	{
		public ILogger CreateLogger(string categoryName)
		{
			return new SentryLogger();
		}

		public void Dispose()
		{
		}
	}
}
