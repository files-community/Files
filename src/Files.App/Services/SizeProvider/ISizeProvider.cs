// Copyright (c) Files Community
// Licensed under the MIT License.

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
