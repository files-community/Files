// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.IO;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.Services.SizeProvider
{
	public sealed partial class CachedSizeProvider : ISizeProvider
	{
		private readonly ConcurrentDictionary<string, ulong> sizes = new();

		public event EventHandler<SizeChangedEventArgs>? SizeChanged;

		public Task CleanAsync() => Task.CompletedTask;

		public Task ClearAsync()
		{
			sizes.Clear();
			return Task.CompletedTask;
		}

		public async Task UpdateAsync(string path, CancellationToken cancellationToken)
		{
			await Task.Yield();
			if (!sizes.ContainsKey(path))
				RaiseSizeChanged(path, 0, SizeChangedValueState.None);

			var stopwatch = Stopwatch.StartNew();
			ulong size = await Calculate(path);

			sizes[path] = size;
			RaiseSizeChanged(path, size, SizeChangedValueState.Final);

			async Task<ulong> Calculate(string path, int level = 0)
			{
				if (string.IsNullOrEmpty(path))
					return 0;

				WIN32_FIND_DATAW findData = default;
				FindCloseSafeHandle hFile;
				unsafe
				{
					hFile = PInvoke.FindFirstFileEx($"{path}{Path.DirectorySeparatorChar}*.*", FINDEX_INFO_LEVELS.FindExInfoBasic,
						&findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
				}
				using FindCloseSafeHandle findHandleScope = hFile;

				ulong size = 0;
				ulong localSize = 0;
				string localPath = string.Empty;

				if (!hFile.IsInvalid)
				{
					do
					{
						string fileName = findData.cFileName.ToString();
						if (((FileAttributes)findData.dwFileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
							// Skip symbolic links and junctions
							continue;

						bool isDirectory = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) is FileAttributes.Directory;
						if (!isDirectory)
						{
							size += (ulong)findData.GetSize();
						}
						else if (fileName is not "." and not "..")
						{
							localPath = Path.Combine(path, fileName);
							localSize = await Calculate(localPath, level + 1);
							size += localSize;
						}

						if (level <= 3)
						{
							await Task.Yield();
							sizes[localPath] = localSize;
						}
						if (level is 0 && stopwatch.ElapsedMilliseconds > 500)
						{
							// Limit updates to every 0.5 seconds to prevent crashes due to frequent updates
							stopwatch.Restart();
							RaiseSizeChanged(path, size, SizeChangedValueState.Intermediate);
						}

						if (cancellationToken.IsCancellationRequested)
							break;
					} while (PInvoke.FindNextFile(hFile, out findData));
				}
				return size;
			}
		}

		public bool TryGetSize(string path, out ulong size) => sizes.TryGetValue(path, out size);

		public void Dispose() { }

		private void RaiseSizeChanged(string path, ulong newSize, SizeChangedValueState valueState)
			=> SizeChanged?.Invoke(this, new SizeChangedEventArgs(path, newSize, valueState));
	}
}
