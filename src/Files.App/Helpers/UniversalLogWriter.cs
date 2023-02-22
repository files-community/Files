using Files.Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using static Files.App.Helpers.NativeFileOperationsHelper;

namespace Files.App.Helpers
{
	/// <summary>
	/// UWP Implementation of ILogger
	/// </summary>
	public class UniversalLogWriter : ILogWriter
	{
		private StorageFile? logFile;
		private bool initialized = false;
		private readonly ConcurrentQueue<string> logsBeforeInit = new();

		public async Task InitializeAsync(string name)
		{
			if (!initialized)
			{
				initialized = true;
				logFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);

				if (logsBeforeInit.Count > 0)
				{
					using var stream = await OpenFileWithRetryAsync(logFile, FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);
					if (stream is null)
						return;
					using var outputStream = stream.GetOutputStreamAt(stream.Size);
					using var dataWriter = new DataWriter(outputStream);
					while (logsBeforeInit.TryDequeue(out var text))
					{
						dataWriter.WriteString("\n" + text);
					}
					await dataWriter.StoreAsync();
					await outputStream.FlushAsync();
				}
			}
		}

		public async Task WriteLineToLogAsync(string text)
		{
			if (logFile is null)
			{
				logsBeforeInit.Enqueue(text);
				return;
			}
			using var stream = await OpenFileWithRetryAsync(logFile, FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);
			if (stream is null)
				return;
			using var outputStream = stream.GetOutputStreamAt(stream.Size);
			using var dataWriter = new DataWriter(outputStream);
			dataWriter.WriteString("\n" + text);
			await dataWriter.StoreAsync();
			await outputStream.FlushAsync();

			Debug.WriteLine($"Logged event: {text}");
		}

		public void WriteLineToLog(string text)
		{
			if (logFile is null)
			{
				logsBeforeInit.Enqueue(text);
				return;
			}
			IntPtr hStream = CreateFileFromApp(logFile.Path,
				GENERIC_WRITE, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, (uint)File_Attributes.BackupSemantics, IntPtr.Zero);
			if (hStream.ToInt64() == -1)
				return;
			byte[] buff = Encoding.UTF8.GetBytes("\n" + text);
			int dwBytesWritten;
			unsafe
			{
				fixed (byte* pBuff = buff)
				{
					SetFilePointer(hStream, 0, IntPtr.Zero, FILE_END);
					WriteFile(hStream, pBuff, buff.Length, &dwBytesWritten, IntPtr.Zero);
				}
			}
			CloseHandle(hStream);

			Debug.WriteLine($"Logged event: {text}");
		}

		private async Task<IRandomAccessStream?> OpenFileWithRetryAsync(IStorageFile2 file, FileAccessMode mode, StorageOpenOptions share, int maxRetries = 5)
		{
			for (int numTries = 0; numTries < maxRetries; numTries++)
			{
				IRandomAccessStream? fs = null;
				try
				{
					fs = await file.OpenAsync(mode, share);
					return fs;
				}
				catch (System.IO.IOException)
				{
					fs?.Dispose();
					await Task.Delay(50);
				}
			}

			return null;
		}
	}
}