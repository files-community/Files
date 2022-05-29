using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.Services.SizeProvider
{
    public class NoSizeProvider : ISizeProvider
    {
        public event EventHandler<SizeChangedEventArgs>? SizeChanged;

        public async Task CleanAsync()
            => await Task.Yield();

        public async Task UpdateAsync(string path, CancellationToken cancellationToken)
            => await Task.Yield();

        public bool TryGetSize(string path, out ulong size)
        {
            size = 0;
            return false;
        }

        public void Dispose() {}
    }
}
