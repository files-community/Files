// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;

namespace Files.App.Utils.Storage
{
	[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
	[DebuggerDisplay("Count = {Count}")]
	[WinRT.GeneratedWinRTExposedType]
	public partial class BulkConcurrentObservableCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged, ICollection<T>, IList<T>, ICollection, IList
	{
		protected bool isBulkOperationStarted;
		private readonly object syncRoot = new object();
		private readonly List<T> collection = [];

		public bool IsSorted { get; set; }

		public int Count
		{
			get
			{
				lock (syncRoot)
				{
					return collection.Count;
				}
			}
		}

		public bool IsReadOnly => false;

		public bool IsFixedSize => false;

		public bool IsSynchronized => true;

		public object SyncRoot => syncRoot;

		protected List<T> Items => collection;

		object? IList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				if (value is not null)
					this[index] = (T)value;
			}
		}

		public T this[int index]
		{
			get
			{
				lock (syncRoot)
				{
					return collection[index];
				}
			}
			set
			{
				T item;
				NotifyCollectionChangedEventArgs e;
				lock (syncRoot)
				{
					item = collection[index];
					collection[index] = value;

					e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, item, index);
					OnCollectionChanged(e, false);
				}

				UpdateGroups(e);
			}
		}

		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		public event PropertyChangedEventHandler? PropertyChanged;

		private Func<T, object>? itemSortKeySelector;

		public Func<T, object>? ItemSortKeySelector
		{
			get => itemSortKeySelector;
			set => itemSortKeySelector = value;
		}

		public BulkConcurrentObservableCollection()
		{
		}

		public BulkConcurrentObservableCollection(IEnumerable<T> items)
		{
			AddRange(items);
		}

		public virtual void BeginBulkOperation()
		{
			lock (syncRoot)
			{
				isBulkOperationStarted = true;
				OnBulkOperationStarted();
			}
		}

		protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e, bool countChanged = true)
		{
			if (!isBulkOperationStarted)
			{
				if (countChanged)
					PropertyChanged?.Invoke(this, EventArgsCache.CountPropertyChanged);

				PropertyChanged?.Invoke(this, EventArgsCache.IndexerPropertyChanged);
				CollectionChanged?.Invoke(this, e);
			}
		}

		protected virtual void UpdateGroups(NotifyCollectionChangedEventArgs e)
		{
		}

		protected virtual void OnBulkOperationStarted()
		{
		}

		protected virtual void OnBulkOperationEnded()
		{
		}

		protected virtual void OnCleared()
		{
		}

		public virtual void EndBulkOperation()
		{
			lock (syncRoot)
			{
				if (!isBulkOperationStarted)
					return;

				isBulkOperationStarted = false;
				OnBulkOperationEnded();

				OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
			}

			UpdateGroups(EventArgsCache.ResetCollectionChanged);
		}

		public void Add(T? item)
		{
			if (item is null)
				return;

			int count;
			NotifyCollectionChangedEventArgs e;

			lock (syncRoot)
			{
				count = collection.Count;
				collection.Add(item);

				e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, count);
				OnCollectionChanged(e);
			}

			UpdateGroups(e);
		}

		public void Clear()
		{
			lock (syncRoot)
			{
				collection.Clear();
				OnCleared();

				OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
			}

			UpdateGroups(EventArgsCache.ResetCollectionChanged);
		}

		public bool Contains(T? item)
		{
			if (item is null)
				return false;

			lock (syncRoot)
			{
				return collection.Contains(item);
			}
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			lock (syncRoot)
			{
				collection.CopyTo(array, arrayIndex);
			}
		}

		public bool Remove(T? item)
		{
			if (item is null)
				return false;

			int index;
			NotifyCollectionChangedEventArgs e;

			lock (syncRoot)
			{
				index = collection.IndexOf(item);

				if (index == -1)
					return false;

				collection.RemoveAt(index);

				e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
				OnCollectionChanged(e);
			}

			UpdateGroups(e);
			return true;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new BlockingListEnumerator<T>(collection, syncRoot);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int IndexOf(T? item)
		{
			if (item is null)
				return -1;

			lock (syncRoot)
			{
				return collection.IndexOf(item);
			}
		}

		public void Insert(int index, T? item)
		{
			if (item is null)
				return;

			NotifyCollectionChangedEventArgs e;

			lock (syncRoot)
			{
				collection.Insert(index, item);

				e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
				OnCollectionChanged(e);
			}

			UpdateGroups(e);
		}

		public void RemoveAt(int index)
		{
			T item;

			NotifyCollectionChangedEventArgs e;

			lock (syncRoot)
			{
				item = collection[index];
				collection.RemoveAt(index);

				e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
				OnCollectionChanged(e);
			}

			UpdateGroups(e);
		}

		public void AddRange(IEnumerable<T> items)
		{
			if (!items.Any())
				return;

			int count;
			NotifyCollectionChangedEventArgs e;

			lock (syncRoot)
			{
				count = collection.Count;
				collection.AddRange(items);

				e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList(), count);
				OnCollectionChanged(e);
			}

			UpdateGroups(e);
		}

		public void InsertRange(int index, IEnumerable<T> items)
		{
			if (!items.Any())
				return;

			NotifyCollectionChangedEventArgs e;

			lock (syncRoot)
			{
				collection.InsertRange(index, items);

				e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList(), index);
				OnCollectionChanged(e);
			}

			UpdateGroups(e);
		}

		public void RemoveRange(int index, int count)
		{
			if (count <= 0)
				return;

			List<T> items;
			NotifyCollectionChangedEventArgs e;

			lock (syncRoot)
			{
				items = collection.Skip(index).Take(count).ToList();
				collection.RemoveRange(index, count);

				e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, index);
				OnCollectionChanged(e);
			}

			UpdateGroups(e);
		}

		public void ReplaceRange(int index, IEnumerable<T> items)
		{
			var count = items.Count();

			if (count == 0)
				return;

			List<T> oldItems;
			List<T> newItems;
			NotifyCollectionChangedEventArgs e;

			lock (syncRoot)
			{
				oldItems = collection.Skip(index).Take(count).ToList();
				newItems = items.ToList();
				collection.RemoveRange(index, count);
				collection.InsertRange(index, newItems);

				e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, index);
				OnCollectionChanged(e, false);
			}

			UpdateGroups(e);
		}

		public void Sort()
		{
			lock (syncRoot)
			{
				collection.Sort();
			}
		}

		public void Sort(Comparison<T> comparison)
		{
			lock (syncRoot)
			{
				collection.Sort(comparison);
			}
		}

		public void Order(Func<List<T>, IEnumerable<T>> func)
		{
			IEnumerable<T> result;
			lock (syncRoot)
			{
				result = func.Invoke(collection);

				ReplaceRange(0, result);
			}
		}

		public void OrderOne(Func<List<T>, IEnumerable<T>> func, T item)
		{
			List<T> result;
			lock (syncRoot)
			{
				result = func.Invoke(collection).ToList();

				Remove(item);
				var index = result.IndexOf(item);
				if (index != -1)
					Insert(index, item);
			}
		}

		int IList.Add(object? value)
		{
			if (value is null)
				return -1;

			int count;
			int index;
			NotifyCollectionChangedEventArgs e;

			lock (syncRoot)
			{
				count = collection.Count;
				index = ((IList)collection).Add((T)value);

				e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, count);
				OnCollectionChanged(e);
			}

			UpdateGroups(e);
			return index;
		}

		bool IList.Contains(object? value)
		{
			return Contains((T?)value);
		}

		int IList.IndexOf(object? value)
		{
			return IndexOf((T?)value);
		}

		void IList.Insert(int index, object? value)
		{
			Insert(index, (T?)value);
		}

		void IList.Remove(object? value)
		{
			Remove((T?)value);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo((T[])array, index);
		}

		private static class EventArgsCache
		{
			internal static readonly PropertyChangedEventArgs CountPropertyChanged = new PropertyChangedEventArgs("Count");
			internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new PropertyChangedEventArgs("Item[]");
			internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
		}
	}
}
