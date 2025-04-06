// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Files.App.Utils.Logger
{
	public sealed partial class SentryLoggerProvider : ILoggerProvider
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
