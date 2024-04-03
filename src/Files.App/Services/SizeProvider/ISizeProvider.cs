// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
