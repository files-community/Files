using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Core.Services.SizeProvider
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
