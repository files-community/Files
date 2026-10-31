// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Files.Shared
{
	public sealed class FileLogger : ILogger, IDisposable
	{
		private const int MaxBatchSize = 256;
		private const int MaxWriteAttempts = 3;
		private const int WriteRetryDelayMilliseconds = 250;

		private readonly string filePath;
		private readonly Channel<string> messages;
		private readonly Task writerTask;
		private bool writerFailed;

		public FileLogger(string filePath)
		{
			this.filePath = filePath;
			messages = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
			{
				SingleReader = true,
				SingleWriter = false,
			});
			writerTask = Task.Run(ProcessLogQueueAsync);
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (formatter is null)
				return;

			try
			{
				var message = LogPathHelper.SanitizeMessage(exception?.ToString() ?? formatter(state, exception));

				messages.Writer.TryWrite($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}|{logLevel}|{message}");
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Writing to log file failed with the following exception:\n{e}");
			}
		}

		private async Task ProcessLogQueueAsync()
		{
			PurgeLogs(100);
			var pendingMessages = new List<string>();
			var consecutiveFailures = 0;

			while (true)
			{
				try
				{
					await using var stream = new FileStream(filePath, new FileStreamOptions
					{
						Mode = FileMode.Append,
						Access = FileAccess.Write,
						Share = FileShare.ReadWrite | FileShare.Delete,
						Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
					});
					await using var writer = new StreamWriter(stream, new UTF8Encoding(false));

					while (pendingMessages.Count > 0 || await messages.Reader.WaitToReadAsync())
					{
						while (pendingMessages.Count < MaxBatchSize && messages.Reader.TryRead(out var message))
							pendingMessages.Add(message);

						foreach (var message in pendingMessages)
							await writer.WriteLineAsync(message);

						await writer.FlushAsync();
						pendingMessages.Clear();
						consecutiveFailures = 0;
					}

					return;
				}
				catch (Exception e)
				{
					Trace.TraceError($"Writing to log file failed with the following exception:\n{e}");
					if (++consecutiveFailures >= MaxWriteAttempts)
					{
						Volatile.Write(ref writerFailed, true);
						messages.Writer.TryComplete();
						pendingMessages.Clear();
						while (messages.Reader.TryRead(out _))
						{
						}
						return;
					}

					await Task.Delay(TimeSpan.FromMilliseconds(WriteRetryDelayMilliseconds * consecutiveFailures));
				}
			}
		}

		private void PurgeLogs(int numberOfLinesKept)
		{
			if (!File.Exists(filePath))
				return;

			try
			{
				var lastLines = File.ReadLines(filePath)
					.TakeLast(numberOfLinesKept + 1)
					.ToArray();
				if (lastLines.Length > numberOfLinesKept)
				{
					File.WriteAllLines(filePath, lastLines.Skip(lastLines.Length - numberOfLinesKept));
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Purging the log file failed with the following exception:\n{e}");
			}
		}

		public void Dispose()
		{
			TryCompleteAndFlush(Timeout.InfiniteTimeSpan);
		}

		public bool TryCompleteAndFlush(TimeSpan timeout)
		{
			messages.Writer.TryComplete();

			try
			{
				return writerTask.Wait(timeout) && !Volatile.Read(ref writerFailed);
			}
			catch (Exception e)
			{
				Trace.TraceError($"Flushing the log file failed with the following exception:\n{e}");
				return false;
			}
		}
	}
}
