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
			int chunkSize = 500; // start small for responsiveness
			const int minChunkSize = 500;
			const int maxChunkSize = 5000;

			var queue = new Queue<string>(_paths);
			var batch = new List<string>(chunkSize);

			int chunksProcessed = 0;

			while (queue.TryDequeue(out var currentPath))
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (!Win32Helper.HasFileAttribute(currentPath, FileAttributes.Directory))
				{
					if (!_computedFiles.ContainsKey(currentPath))
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
								if (!_computedFiles.ContainsKey(entry))
									batch.Add(entry);
							}

							if (batch.Count >= chunkSize)
							{
								await ProcessBatchAsync(batch, cancellationToken).ConfigureAwait(false);

								// ✅ Adaptive tuning
								if (++chunksProcessed % 5 == 0)
								{
									if (chunkSize < maxChunkSize)
										chunkSize = Math.Min(chunkSize * 2, maxChunkSize);
								}
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

				if (batch.Count >= chunkSize)
				{
					await ProcessBatchAsync(batch, cancellationToken).ConfigureAwait(false);
				}
			}

			// Process any remaining files
			if (batch.Count > 0)
			{
				await ProcessBatchAsync(batch, cancellationToken).ConfigureAwait(false);
			}

			Completed = true;
		}

		private async Task ProcessBatchAsync(List<string> batch, CancellationToken token)
		{
			if (batch.Count == 0) return;

			var files = batch.ToArray();
			batch.Clear();

			long batchTotal = await Task.Run(() =>
			{
				long localSum = 0;

				Parallel.ForEach(
					files,
					new ParallelOptions
					{
						MaxDegreeOfParallelism = Environment.ProcessorCount,
						CancellationToken = token
					},
					file =>
					{
						try
						{
							using var hFile = PInvoke.CreateFile(
								file,
								(uint)FILE_ACCESS_RIGHTS.FILE_READ_ATTRIBUTES,
								FILE_SHARE_MODE.FILE_SHARE_READ,
								null,
								FILE_CREATION_DISPOSITION.OPEN_EXISTING,
								0,
								null);

							if (!hFile.IsInvalid && PInvoke.GetFileSizeEx(hFile, out long size))
							{
								if (_computedFiles.TryAdd(file, size))
									Interlocked.Add(ref localSum, size);
							}
						}
						catch { /* ignore bad files */ }
					});

				return localSum;
			}, token).ConfigureAwait(false);

			if (batchTotal > 0)
				Interlocked.Add(ref _size, batchTotal);

			// Yield to UI to avoid freezing
			await Task.Yield();
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
