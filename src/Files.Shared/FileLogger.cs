using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Files.Shared
{
	public class FileLogger : ILogger
	{
		private readonly SemaphoreSlim semaphoreSlim = new(1);
		private readonly string filePath;

		public FileLogger(string filePath)
		{
			this.filePath = filePath;
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (formatter is not null)
			{
				semaphoreSlim.Wait();
				try
				{
					File.AppendAllText(filePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}|{logLevel}|{formatter(state, exception)}" + Environment.NewLine);
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Writing to log file failed with the following exception:\n{e}");
				}
				finally
				{
					semaphoreSlim.Release();
				}
			}
		}
	}
}
