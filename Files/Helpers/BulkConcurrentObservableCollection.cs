using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Files.Helpers
{
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public class BulkConcurrentObservableCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged, ICollection<T>, IList<T>, ICollection, IList
    {
        private bool isBulkOperationStarted;
        private readonly object syncRoot = new object();
        private readonly List<T> collection = new List<T>();

        public int Count => collection.Count;

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => true;

        public object SyncRoot => syncRoot;

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
            get => collection[index];
            set
            {
                var item = collection[index];
                collection[index] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, item, index), false);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public void BeginBulkOperation()
        {
            isBulkOperationStarted = true;
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e, bool countChanged = true)
        {
            if (!isBulkOperationStarted)
            {
                CollectionChanged?.Invoke(this, e);
                if (countChanged)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            }
        }

        public void EndBulkOperation()
        {
            isBulkOperationStarted = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }

        public void Add(T item)
        {
            lock (syncRoot)
            {
                collection.Add(item);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Clear()
        {
            lock (syncRoot)
            {
                collection.Clear();
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return collection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            collection.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            int index;

            lock (syncRoot)
            {
                index = collection.IndexOf(item);

                if (index == -1)
                {
                    return true;
                }

                collection.RemoveAt(index);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return collection.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            lock (syncRoot)
            {
                collection.Insert(index, item);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void RemoveAt(int index)
        {
            T item;

            lock (syncRoot)
            {
                item = collection[index];
                collection.RemoveAt(index);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items.Count() == 0)
            {
                return;
            }

            lock (syncRoot)
            {
                collection.AddRange(items);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList()));
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            if (items.Count() == 0)
            {
                return;
            }

            lock (syncRoot)
            {
                collection.InsertRange(index, items);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList(), index));
        }

        public void RemoveRange(int index, int count)
        {
            if (count <= 0)
            {
                return;
            }

            List<T> items;

            lock (syncRoot)
            {
                items = collection.Skip(index).Take(count).ToList();
                collection.RemoveRange(index, count);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, index));
        }

        public void ReplaceRange(int index, IEnumerable<T> items)
        {
            var count = items.Count();

            if (count == 0)
            {
                return;
            }

            List<T> oldItems;
            List<T> newItems;

            lock (syncRoot)
            {
                oldItems = collection.Skip(index).Take(count).ToList();
                newItems = items.ToList();
                collection.InsertRange(index, newItems);
                collection.RemoveRange(index + count, count);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, index));
        }

        int IList.Add(object value)
        {
            int index;

            lock (syncRoot)
            {
                index = ((IList)collection).Add((T)value);
            }

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