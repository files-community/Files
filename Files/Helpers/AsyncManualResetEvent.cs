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

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            var tcs = m_tcs;
            cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetCanceled(), tcs);

            return m_tcs.Task;
        }

        private async Task<bool> Delay(int milliseconds)
        {
            await Task.Delay(milliseconds);
            return false;
        }

        public Task<bool> WaitAsync(int milliseconds, CancellationToken cancellationToken = default)
        {
            var tcs = m_tcs;

            cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetCanceled(), tcs);


            return Task.WhenAny(m_tcs.Task, Delay(milliseconds)).Result;
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
