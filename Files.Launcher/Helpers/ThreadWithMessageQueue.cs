using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FilesFullTrust.Helpers
{
    public class ThreadWithMessageQueue<T> : IDisposable
    {
        private BlockingCollection<Internal> messageQueue;
        private Thread thread;
        private DisposableDictionary state;

        public void Dispose()
        {
            messageQueue.CompleteAdding();
            thread.Join();
            state.Dispose();
        }

        public async Task<V> PostMessageAsync<V>(T payload)
        {
            var message = new Internal(payload);
            messageQueue.TryAdd(message);
            return (V)await message.tcs.Task;
        }

        public Task PostMessage(T payload)
        {
            var message = new Internal(payload);
            messageQueue.TryAdd(message);
            return message.tcs.Task;
        }

        public ThreadWithMessageQueue(Func<T, DisposableDictionary, object> handleMessage)
        {
            messageQueue = new BlockingCollection<Internal>(new ConcurrentQueue<Internal>());
            state = new DisposableDictionary();
            thread = new Thread(new ThreadStart(() =>
            {
                foreach (var message in messageQueue.GetConsumingEnumerable())
                {
                    var res = handleMessage(message.payload, state);
                    message.tcs.SetResult(res);
                }
            }));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private class Internal
        {
            public T payload;
            public TaskCompletionSource<object> tcs;

            public Internal(T payload)
            {
                this.payload = payload;
                this.tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }

    public class DisposableDictionary : IDisposable
    {
        private ConcurrentDictionary<string, object> dict;

        public DisposableDictionary()
        {
            dict = new ConcurrentDictionary<string, object>();
        }

        public string AddValue(object obj)
        {
            string key = Guid.NewGuid().ToString();
            if (!dict.TryAdd(key, obj))
            {
                throw new ArgumentException("Could not create handle: key exists");
            }

            return key;
        }

        public void SetValue(string key, object obj)
        {
            RemoveValue(key);
            if (!dict.TryAdd(key, obj))
            {
                throw new ArgumentException("Could not create handle: key exists");
            }
        }

        public object GetValue(string key)
        {
            dict.TryGetValue(key, out var elem);
            return elem;
        }

        public T GetValue<T>(string key)
        {
            dict.TryGetValue(key, out var elem);
            return (T)elem;
        }

        public void RemoveValue(string key)
        {
            dict.TryRemove(key, out var elem);
            (elem as IDisposable)?.Dispose();
        }

        public void Dispose()
        {
            foreach (var elem in dict)
            {
                dict.TryRemove(elem.Key, out _);
                (elem.Value as IDisposable)?.Dispose();
            }
        }
    }
}
