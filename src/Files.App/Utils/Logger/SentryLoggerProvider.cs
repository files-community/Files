// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
