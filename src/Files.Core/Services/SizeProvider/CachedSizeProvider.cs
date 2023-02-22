using Files.Core.Extensions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Files.Core.Helpers.NativeFindStorageItemHelper;

namespace Files.Core.Services.SizeProvider
{
	public class CachedSizeProvider : ISizeProvider
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
			if (sizes.TryGetValue(path, out ulong cachedSize))
			{
				RaiseSizeChanged(path, cachedSize, SizeChangedValueState.Final);
			}
			else
			{
				RaiseSizeChanged(path, 0, SizeChangedValueState.None);
			}

			ulong size = await Calculate(path);

			sizes[path] = size;
			RaiseSizeChanged(path, size, SizeChangedValueState.Final);

			async Task<ulong> Calculate(string path, int level = 0)
			{
				if (string.IsNullOrEmpty(path))
				{
					return 0;
				}

				IntPtr hFile = FindFirstFileExFromApp($"{path}{Path.DirectorySeparatorChar}*.*", FINDEX_INFO_LEVELS.FindExInfoBasic,
					out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

				ulong size = 0;
				ulong localSize = 0;
				string localPath = string.Empty;

				if (hFile.ToInt64() is not -1)
				{
					do
					{
						bool isDirectory = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) is FileAttributes.Directory;
						if (!isDirectory)
						{
							size += (ulong)findData.GetSize();
						}
						else if (findData.cFileName is not "." and not "..")
						{
							localPath = Path.Combine(path, findData.cFileName);
							localSize = await Calculate(localPath, level + 1);
							size += localSize;
						}

						if (level <= 3)
						{
							await Task.Yield();
							sizes[localPath] = localSize;
						}
						if (level is 0)
						{
							RaiseSizeChanged(path, size, SizeChangedValueState.Intermediate);
						}

						if (cancellationToken.IsCancellationRequested)
						{
							break;
						}
					} while (FindNextFile(hFile, out findData));
					FindClose(hFile);
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
