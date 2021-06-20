using Files.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Files.Helpers
{
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public class BulkConcurrentObservableCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged, ICollection<T>, IList<T>, ICollection, IList
    {
        protected bool isBulkOperationStarted;
        private readonly object syncRoot = new object();
        private readonly List<T> collection = new List<T>();

        public BulkConcurrentObservableCollection<GroupedCollection<T>> GroupedCollection { get; private set; }
        public bool IsSorted { get; set; }

        public int Count => collection.Count;

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => true;

        public object SyncRoot => syncRoot;

        public bool IsGrouped => !(ItemGroupKeySelector is null);

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

        private Func<T, string> itemGroupKeySelector;

        public Func<T, string> ItemGroupKeySelector
        {
            get => itemGroupKeySelector;
            set
            {
                itemGroupKeySelector = value;
                if (value != null)
                {
                    GroupedCollection ??= new BulkConcurrentObservableCollection<GroupedCollection<T>>();
                }
                else
                {
                    GroupedCollection = null;
                }
            }
        }

        private Func<T, object> itemSortKeySelector;

        public Func<T, object> ItemSortKeySelector
        {
            get => itemSortKeySelector;
            set => itemSortKeySelector = value;
        }

        public Action<GroupedCollection<T>> GetGroupHeaderInfo { get; set; }
        public Action<GroupedCollection<T>> GetExtendedGroupHeaderInfo { get; set; }

        public BulkConcurrentObservableCollection()
        {
        }

        public BulkConcurrentObservableCollection(IEnumerable<T> items)
        {
            AddRange(items);
        }

        public virtual void BeginBulkOperation()
        {
            isBulkOperationStarted = true;
            GroupedCollection?.ForEach(gp => gp.BeginBulkOperation());
            GroupedCollection?.BeginBulkOperation();
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e, bool countChanged = true)
        {
            if (!isBulkOperationStarted)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                CollectionChanged?.Invoke(this, e);
                if (countChanged)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                }
            }

            if (IsGrouped)
            {
                if (!(e.NewItems is null))
                {
                    AddItemsToGroup(e.NewItems.Cast<T>());
                }
                if (!(e.OldItems is null))
                {
                    RemoveItemsFromGroup(e.OldItems.Cast<T>());
                }
            }
        }

        public void ResetGroups(CancellationToken token = default)
        {
            if (!IsGrouped)
            {
                return;
            }

            // Prevents any unwanted errors caused by bindings updating
            GroupedCollection.ForEach(x => x.Model.PausePropertyChangedNotifications());
            GroupedCollection.Clear();
            AddItemsToGroup(collection, token);
        }

        private void AddItemsToGroup(IEnumerable<T> items, CancellationToken token = default)
        {
            foreach (var item in items)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                var key = GetGroupKeyForItem(item);
                var groups = GroupedCollection.Where(x => x.Model.Key == key);
                if (item is IGroupableItem groupable)
                {
                    groupable.Key = key;
                }
                if (groups.Count() > 0)
                {
                    var gp = groups.First();
                    gp.Add(item);
                    gp.IsSorted = false;
                }
                else
                {
                    var group = new GroupedCollection<T>(key)
                    {
                        item
                    };

                    group.GetExtendedGroupHeaderInfo = GetExtendedGroupHeaderInfo;
                    if (!(GetGroupHeaderInfo is null))
                    {
                        GetGroupHeaderInfo.Invoke(group);
                    }
                    GroupedCollection.Add(group);
                    GroupedCollection.IsSorted = false;
                }
            }
        }

        private void RemoveItemsFromGroup(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                var key = GetGroupKeyForItem(item);

                var groups = GroupedCollection.Where(x => x.Model.Key == key);
                if (groups.Count() > 0)
                {
                    groups.First().Remove(item);
                }
            }
        }

        private string GetGroupKeyForItem(T item)
        {
            return ItemGroupKeySelector?.Invoke(item);
        }

        public virtual void EndBulkOperation()
        {
            if (!isBulkOperationStarted)
            {
                return;
            }
            isBulkOperationStarted = false;
            GroupedCollection?.ForEach(gp => gp.EndBulkOperation());
            GroupedCollection?.EndBulkOperation();

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
            GroupedCollection?.Clear();

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

        public void Sort()
        {
            lock (SyncRoot)
            {
                collection.Sort();
            }
        }

        public void Sort(Comparison<T> comparison)
        {
            lock (SyncRoot)
            {
                collection.Sort(comparison);
            }
        }

        public void Order(Func<List<T>, IEnumerable<T>> func)
        {
            IEnumerable<T> result;
            lock (SyncRoot)
            {
                result = func.Invoke(collection);
            }

            ReplaceRange(0, result);
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