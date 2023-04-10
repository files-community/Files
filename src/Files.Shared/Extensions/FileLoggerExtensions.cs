// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;

namespace Files.Shared.Extensions
{
	public static class FileLoggerExtensions
	{
		public static ILoggerFactory AddFile(this ILoggerFactory factory, string filePath)
		{
			factory.AddProvider(new FileLoggerProvider(filePath));
			return factory;
		}
	}
}
