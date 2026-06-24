// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.SearchService.Watch;

/// <summary>
/// Deduplicates and debounces filesystem events before applying them
/// to the index. Coalesces bursts (git checkout, zip extract) into a
/// single batch committed after a 250ms quiet window.
/// </summary>
internal sealed class EventBatcher : IDisposable
{
	private const int DebounceMs = 250;

	private readonly Action<IReadOnlyList<PendingChange>> _onBatch;
	private readonly Dictionary<string, PendingChange> _pending = new(StringComparer.OrdinalIgnoreCase);
	private readonly Lock _lock = new();
	private Timer? _timer;

	public EventBatcher(Action<IReadOnlyList<PendingChange>> onBatch) => _onBatch = onBatch;

	public void Enqueue(PendingChange change)
	{
		lock (_lock)
		{
			// Last event for a given path wins — a delete after a create = delete.
			_pending[change.FullPath] = change;
			_timer?.Dispose();
			_timer = new Timer(_ => Flush(), null, DebounceMs, Timeout.Infinite);
		}
	}

	private void Flush()
	{
		List<PendingChange> batch;
		lock (_lock)
		{
			if (_pending.Count == 0) return;
			batch = [.. _pending.Values];
			_pending.Clear();
		}
		_onBatch(batch);
	}

	public void Dispose()
	{
		_timer?.Dispose();
		Flush();
	}
}

internal readonly record struct PendingChange(string FullPath, ChangeKind Kind);

internal enum ChangeKind { Upsert, Delete }
