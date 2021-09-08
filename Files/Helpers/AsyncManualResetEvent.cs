using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();

        public async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            var tcs = m_tcs;
            var cancelTcs = new TaskCompletionSource<bool>();

            cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetCanceled(), cancelTcs);

            await await Task.WhenAny(tcs.Task, cancelTcs.Task);
        }

        private async Task<bool> Delay(int milliseconds)
        {
            await Task.Delay(milliseconds);
            return false;
        }

        public async Task<bool> WaitAsync(int milliseconds, CancellationToken cancellationToken = default)
        {
            var tcs = m_tcs;
            var cancelTcs = new TaskCompletionSource<bool>();

            cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetCanceled(), cancelTcs);

            return await await Task.WhenAny(tcs.Task, cancelTcs.Task, Delay(milliseconds));
        }

        public void Set()
        {
            var tcs = m_tcs;
            Task.Factory.StartNew(s => ((TaskCompletionSource<bool>)s).TrySetResult(true),
                tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
            tcs.Task.Wait();
        }

        public void Reset()
        {
            while (true)
            {
                var tcs = m_tcs;
                if (!tcs.Task.IsCompleted ||
                    Interlocked.CompareExchange(ref m_tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                    return;
            }
        }
    }
}
