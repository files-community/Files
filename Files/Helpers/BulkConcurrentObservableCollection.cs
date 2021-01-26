using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Files.Helpers
{
    public class BulkConcurrentObservableCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged, ICollection<T>, IList<T>, ICollection, IList
    {
        private volatile bool isBulkOperationStarted;
        private volatile int readerCount;
        private volatile bool needSnapshot;
        private readonly SemaphoreSlim writerLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim snapshotLock = new SemaphoreSlim(1, 1);
        private readonly List<T> collection = new List<T>();
        private List<T> snapshot = new List<T>();

        private void Write(Action writeFunc)
        {
            writerLock.Wait();
            try
            {
                writeFunc();
            }
            finally
            {
                writerLock.Release();
                snapshotLock.Wait();
                needSnapshot = true;
                snapshotLock.Release();
            }
        }

        private U Write<U>(Func<U> writeFunc)
        {
            writerLock.Wait();
            try
            {
                return writeFunc();
            }
            finally
            {
                writerLock.Release();
                snapshotLock.Wait();
                needSnapshot = true;
                snapshotLock.Release();
            }
        }

        private void Read(Action readFunc)
        {
            mutex.Wait();
            readerCount++;
            if (readerCount == 1)
            {
                writerLock.Wait();
            }
            mutex.Release();
            try
            {
                readFunc();
            }
            finally
            {
                mutex.Wait();
                readerCount--;
                if (readerCount == 0)
                {
                    writerLock.Release();
                }
                mutex.Release();
            }
        }

        private U Read<U>(Func<U> readFunc)
        {
            mutex.Wait();
            readerCount++;
            if (readerCount == 1)
            {
                writerLock.Wait();
            }
            mutex.Release();
            try
            {
                return readFunc();
            }
            finally
            {
                mutex.Wait();
                readerCount--;
                if (readerCount == 0)
                {
                    writerLock.Release();
                }
                mutex.Release();
            }
        }

        public int Count => Read(() => collection.Count);

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => false;

        public object SyncRoot => throw new NotImplementedException();

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = (T)value;
            }
        }

        public T this[int index]
        {
            get => Read(() => collection[index]);
            set
            {
                var item = Write(() =>
                {
                    var item = collection[index];
                    collection[index] = value;
                    return item;
                });
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, item));
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public void BeginBulkOperation()
        {
            isBulkOperationStarted = true;
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!isBulkOperationStarted)
            {
                CollectionChanged?.Invoke(this, e);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            }
        }

        public void EndBulkOperation()
        {
            isBulkOperationStarted = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Add(T item)
        {
            Write(() => collection.Add(item));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Clear()
        {
            Write(() => collection.Clear());
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return Read(() => collection.Contains(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Read(() => collection.CopyTo(array, arrayIndex));
        }

        public bool Remove(T item)
        {
            var result = Write(() => collection.Remove(item));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            snapshotLock.Wait();
            if (needSnapshot)
            {
                snapshot = Read(() => collection.ToList());
                needSnapshot = false;
            }
            snapshotLock.Release();
            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return Read(() => collection.IndexOf(item));
        }

        public void Insert(int index, T item)
        {
            Write(() => collection.Insert(index, item));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void RemoveAt(int index)
        {
            var item = Write(() =>
            {
                var item = collection[index];
                collection.RemoveAt(index);
                return item;
            });
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items.Count() == 0)
            {
                return;
            }
            Write(() => collection.AddRange(items));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList()));
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            if (items.Count() == 0)
            {
                return;
            }
            Write(() => collection.InsertRange(index, items));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList(), index));
        }

        public void RemoveRange(int index, int count)
        {
            if (count == 0)
            {
                return;
            }
            var items = Write(() =>
            {
                var items = collection.Skip(index).Take(count).ToList();
                collection.RemoveRange(index, count);
                return items;
            });
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items));
        }

        public void ReplaceRange(int index, IEnumerable<T> items)
        {
            var count = items.Count();
            if (count == 0)
            {
                return;
            }
            var (newItems, oldItems) = Write(() =>
            {
                var oldItems = collection.Skip(index).Take(count).ToList();
                var newItems = items.ToList();
                collection.InsertRange(index, newItems);
                collection.RemoveRange(index + count, count);
                return (newItems, oldItems);
            });
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems));
        }

        int IList.Add(object value)
        {
            var index = Write(() =>
            {
                collection.Add((T)value);
                return collection.Count;
            });
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
            return index;
        }

        bool IList.Contains(object value) => Contains((T)value);
        int IList.IndexOf(object value) => IndexOf((T)value);
        void IList.Insert(int index, object value) => Insert(index, (T)value);
        void IList.Remove(object value) => Remove((T)value);
        void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);
    }
}