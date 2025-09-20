// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.IO;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.Utils.Storage.Operations
{
	internal sealed class FileSizeCalculator
	{
		private readonly string[] _paths;
		private readonly ConcurrentDictionary<string, long> _computedFiles = new();
		private long _size;

		public long Size => _size;
		public int ItemsCount => _computedFiles.Count;
		public bool Completed { get; private set; }

		public FileSizeCalculator(params string[] paths)
		{
			_paths = paths;
		}

		public async Task ComputeSizeAsync(CancellationToken cancellationToken = default)
		{
			const int ChunkSize = 1000;
			var queue = new Queue<string>(_paths);
			var batch = new List<string>(ChunkSize);

		while (queue.TryDequeue(out var currentPath))
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!Win32Helper.HasFileAttribute(currentPath, FileAttributes.Directory))
			{
				batch.Add(currentPath);
			}
			else
			{
				try
				{
					// Use EnumerateFileSystemEntries to get both files and directories in one pass
					foreach (var entry in Directory.EnumerateFileSystemEntries(currentPath))
					{
						cancellationToken.ThrowIfCancellationRequested();

						if (Win32Helper.HasFileAttribute(entry, FileAttributes.Directory))
						{
							queue.Enqueue(entry);
						}
						else
						{
							batch.Add(entry);
						}

						if (batch.Count >= ChunkSize)
						{
							await ProcessBatchAsync(batch, ChunkSize);
						}
					}
				}
				catch (UnauthorizedAccessException) { }
				catch (IOException) { }
#if DEBUG
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex);
				}
#endif
			}

			if (batch.Count >= ChunkSize)
			{
				await ProcessBatchAsync(batch, ChunkSize);
			}
		}

		// Process remaining items
		if (batch.Count > 0)
		{
			ComputeFileSizeBatch(batch);
			batch.Clear();
		}

		Completed = true;
	}

	private async Task ProcessBatchAsync(List<string> batch, int chunkSize)
	{
		ComputeFileSizeBatch(batch);
		batch.Clear();
		await Task.Yield();
	}

		private void ComputeFileSizeBatch(IEnumerable<string> files)
		{
			long batchTotal = 0;
			foreach (var path in files)
			{
				if (_computedFiles.ContainsKey(path))
					continue;

				using var hFile = PInvoke.CreateFile(
					path,
					(uint)FILE_ACCESS_RIGHTS.FILE_READ_ATTRIBUTES,
					FILE_SHARE_MODE.FILE_SHARE_READ,
					null,
					FILE_CREATION_DISPOSITION.OPEN_EXISTING,
					0,
					null);

				if (!hFile.IsInvalid && PInvoke.GetFileSizeEx(hFile, out long size))
				{
					if (_computedFiles.TryAdd(path, size))
						batchTotal += size;
				}
			}

			if (batchTotal > 0)
				Interlocked.Add(ref _size, batchTotal);
		}

		private long ComputeFileSize(string path)
		{
			if (_computedFiles.TryGetValue(path, out var size))
				return size;

			using var hFile = PInvoke.CreateFile(
				path,
				(uint)FILE_ACCESS_RIGHTS.FILE_READ_ATTRIBUTES,
				FILE_SHARE_MODE.FILE_SHARE_READ,
				null,
				FILE_CREATION_DISPOSITION.OPEN_EXISTING,
				0,
				null);

			if (!hFile.IsInvalid && PInvoke.GetFileSizeEx(hFile, out size) && _computedFiles.TryAdd(path, size))
				Interlocked.Add(ref _size, size);

			return size;
		}

		public void ForceComputeFileSize(string path)
		{
			if (!Win32Helper.HasFileAttribute(path, FileAttributes.Directory))
				ComputeFileSize(path);
		}

		public bool TryGetComputedFileSize(string path, out long size)
		{
			return _computedFiles.TryGetValue(path, out size);
		}
	}
}
