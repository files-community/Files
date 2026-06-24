// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.SearchService.Index;

namespace Files.SearchService.Watch;

/// <summary>
/// Watches the indexed root for filesystem changes and applies them to
/// the index via <see cref="EventBatcher"/>. Uses <see cref="FileSystemWatcher"/>
/// which wraps ReadDirectoryChangesW on Windows.
/// </summary>
internal sealed class ChangeWatcher : IDisposable
{
	private readonly FileSystemWatcher _watcher;
	private readonly EventBatcher _batcher;
	private readonly FileIndex _index;

	/// <summary>
	/// Fired when the watcher's internal buffer overflows and events were lost.
	/// The caller should stop the watcher, re-enumerate, and restart.
	/// </summary>
	public event Action? Overflow;

	public ChangeWatcher(string root, FileIndex index)
	{
		_index = index;
		_batcher = new EventBatcher(ApplyBatch);
		_watcher = new FileSystemWatcher(root)
		{
			IncludeSubdirectories = true,
			NotifyFilter =
				NotifyFilters.FileName |
				NotifyFilters.DirectoryName |
				NotifyFilters.LastWrite |
				NotifyFilters.Size,
			InternalBufferSize = 65536,
		};

		_watcher.Created += (_, e) => _batcher.Enqueue(new PendingChange(e.FullPath, ChangeKind.Upsert));
		_watcher.Changed += (_, e) => _batcher.Enqueue(new PendingChange(e.FullPath, ChangeKind.Upsert));
		_watcher.Deleted += (_, e) => _batcher.Enqueue(new PendingChange(e.FullPath, ChangeKind.Delete));
		_watcher.Renamed += (_, e) =>
		{
			_batcher.Enqueue(new PendingChange(e.OldFullPath, ChangeKind.Delete));
			_batcher.Enqueue(new PendingChange(e.FullPath, ChangeKind.Upsert));
		};
		_watcher.Error += (_, e) =>
		{
			var ex = e.GetException();
			if (ex is InternalBufferOverflowException)
				Overflow?.Invoke();
			else
				Console.Error.WriteLine($"[watcher] error: {ex.Message}");
		};
	}

	public void Start() => _watcher.EnableRaisingEvents = true;
	public void Stop() => _watcher.EnableRaisingEvents = false;

	private void ApplyBatch(IReadOnlyList<PendingChange> batch)
	{
		foreach (var change in batch)
		{
			if (change.Kind == ChangeKind.Delete)
			{
				_index.Delete(change.FullPath);
				continue;
			}

			try
			{
				var fi = new FileInfo(change.FullPath);
				if (!fi.Exists || fi.Attributes.HasFlag(FileAttributes.ReparsePoint))
					continue;
				if (fi.Attributes.HasFlag(FileAttributes.Directory))
					continue;

				_index.Upsert(fi.FullName, fi.Name, (ulong)fi.Length, fi.LastWriteTimeUtc);
			}
			catch (IOException) { } // Race: file deleted between event and stat.
		}
	}

	public void Dispose()
	{
		_watcher.Dispose();
		_batcher.Dispose();
	}
}
