// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Services.SizeProvider
{
	public interface ISizeProvider : IDisposable
	{
		event EventHandler<SizeChangedEventArgs> SizeChanged;

		Task CleanAsync();
		Task ClearAsync();

		Task UpdateAsync(string path, CancellationToken cancellationToken);
		bool TryGetSize(string path, out ulong size);
	}
}
