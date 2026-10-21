// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

internal sealed class CachedTask<T>(Func<Task<T>> load, TimeSpan lifetime, Func<bool>? hasChanged = null, bool allowNull = false)
{
	private readonly Lock gate = new();
	private Task<T>? pending;
	private long expiresAt;
	private bool invalidated;

	internal async Task<T> GetAsync()
	{
		var checkChanges = true;
		while (true)
		{
			Task<T> current;
			lock (gate)
			{
				if (checkChanges)
				{
					if (hasChanged?.Invoke() ?? false)
						invalidated = true;
					checkChanges = false;
				}

				if (pending is null || (pending.IsCompleted && (!pending.IsCompletedSuccessfully || invalidated || Environment.TickCount64 >= expiresAt)))
				{
					invalidated = false;
					pending = Task.Run(async () =>
					{
						var result = await load().ConfigureAwait(false);
						if (result is null && !allowNull)
							throw new InvalidOperationException("Initialization did not produce a snapshot.");
						lock (gate)
							expiresAt = Environment.TickCount64 + (long)lifetime.TotalMilliseconds;
						return result;
					});
				}
				current = pending;
			}

			var value = await current.ConfigureAwait(false);
			lock (gate)
			{
				// A refresh invalidated during enumeration must finish before callers see it.
				if (ReferenceEquals(current, pending) && !invalidated)
					return value;
			}
		}
	}

	internal void Invalidate()
	{
		lock (gate)
			invalidated = true;
	}
}
