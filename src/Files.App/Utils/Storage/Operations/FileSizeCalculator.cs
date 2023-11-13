using System.Collections.Concurrent;
using System.IO;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace Files.App.Utils.Storage.Operations
{
	internal class FileSizeCalculator
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
			await Parallel.ForEachAsync(_paths, cancellationToken, async (path, token) => await Task.Factory.StartNew(() =>
			{
				var queue = new Queue<string>();
				if (!NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Directory))
				{
					ComputeFileSize(path);
				}
				else
				{
					queue.Enqueue(path);

					while (queue.TryDequeue(out var directory))
					{
						using var hFile = FindFirstFileEx(
							directory + "\\*.*",
							FINDEX_INFO_LEVELS.FindExInfoBasic,
							out WIN32_FIND_DATA findData,
							FINDEX_SEARCH_OPS.FindExSearchNameMatch,
							IntPtr.Zero,
							FIND_FIRST.FIND_FIRST_EX_LARGE_FETCH);

						if (!hFile.IsInvalid)
						{
							do
							{
								if ((findData.dwFileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
									// Skip symbolic links and junctions
									continue;

								var itemPath = Path.Combine(directory, findData.cFileName);

								if ((findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
								{
									ComputeFileSize(itemPath);
								}
								else if (findData.cFileName != "." && findData.cFileName != "..")
								{
									queue.Enqueue(itemPath);
								}

								if (token.IsCancellationRequested)
									break;
							}
							while (FindNextFile(hFile, out findData));
						}
					}
				}
			}, token, TaskCreationOptions.LongRunning, TaskScheduler.Default));
		}

		private long ComputeFileSize(string path)
		{
			if (_computedFiles.TryGetValue(path, out var size))
			{
				return size;
			}

			using var hFile = CreateFile(
				path,
				Kernel32.FileAccess.FILE_READ_ATTRIBUTES,
				FileShare.Read,
				null,
				FileMode.Open,
				0,
				null);

			if (!hFile.IsInvalid)
			{
				if (GetFileSizeEx(hFile, out size) && _computedFiles.TryAdd(path, size))
				{
					Interlocked.Add(ref _size, size);
				}
			}

			return size;
		}

		public void ForceComputeFileSize(string path)
		{
			if (!NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Directory))
			{
				ComputeFileSize(path);
			}
		}

		public bool TryGetComputedFileSize(string path, out long size)
		{
			return _computedFiles.TryGetValue(path, out size);
		}
	}
}
