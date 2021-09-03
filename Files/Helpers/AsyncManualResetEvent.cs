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
            using (cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetCanceled(), tcs)) { }

            return m_tcs.Task;
        }

        public Task<bool> WaitAsync(int milliseconds, CancellationToken cancellationToken = default)
        {
            var tcs = m_tcs;

            using (cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetCanceled(), tcs)) { }

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(milliseconds);
            using (cancellationTokenSource.Token.Register(
                s =>
                {
                    var l_tcs = (TaskCompletionSource<bool>)s;

                    if (!l_tcs.Task.IsCanceled)
                    {
                        l_tcs.TrySetResult(false);
                    }

                    cancellationTokenSource.Dispose();
                }, tcs)) { }

            return m_tcs.Task;
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
