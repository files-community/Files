// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.Services.SizeProvider
{
	public class NoSizeProvider : ISizeProvider
	{
		public event EventHandler<SizeChangedEventArgs>? SizeChanged;

		public Task CleanAsync() => Task.CompletedTask;
		public Task ClearAsync() => Task.CompletedTask;

		public Task UpdateAsync(string path, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public bool TryGetSize(string path, out ulong size)
		{
			size = 0;
			return false;
		}

		public void Dispose() { }
	}
}
