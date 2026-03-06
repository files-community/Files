// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;

namespace Files.App.Controls
{
	[DebuggerDisplay("Count = {Count}")]
	public class BulkConcurrentObservableCollection<T> : ObservableCollection<T>
	{
		private readonly object _syncRoot = new();
		private int _deferLevel;
		private bool _hasDeferredChanges;

		public object SyncRoot => _syncRoot;

		public BulkConcurrentObservableCollection()
		{
		}

		public BulkConcurrentObservableCollection(IEnumerable<T> collection)
			: base(collection ?? throw new ArgumentNullException(nameof(collection)))
		{
		}

		public IDisposable BeginBulkOperation()
		{
			lock (_syncRoot)
			{
				_deferLevel++;
			}

			return new BulkOperation(this);
		}

		public void EndBulkOperation()
		{
			bool shouldReset;
			lock (_syncRoot)
			{
				if (_deferLevel == 0)
					return;

				_deferLevel--;
				shouldReset = _deferLevel == 0 && _hasDeferredChanges;
				if (shouldReset)
					_hasDeferredChanges = false;
			}

			if (shouldReset)
				RaiseReset();
		}

		public void AddRange(IEnumerable<T> items)
		{
			ArgumentNullException.ThrowIfNull(items);
			var materialized = Materialize(items);
			if (materialized.Count == 0)
				return;

			lock (_syncRoot)
			{
				CheckReentrancy();
				var startIndex = Items.Count;
				foreach (var item in materialized)
				{
					Items.Add(item);
				}

				OnCountPropertyChanged();
				OnIndexerPropertyChanged();
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, materialized, startIndex));
			}
		}

		public void InsertRange(int index, IEnumerable<T> items)
		{
			ArgumentNullException.ThrowIfNull(items);
			var materialized = Materialize(items);
			if (materialized.Count == 0)
				return;

			lock (_syncRoot)
			{
				CheckReentrancy();
				if (index < 0 || index > Items.Count)
					throw new ArgumentOutOfRangeException(nameof(index));

				for (var i = 0; i < materialized.Count; i++)
				{
					Items.Insert(index + i, materialized[i]);
				}

				OnCountPropertyChanged();
				OnIndexerPropertyChanged();
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, materialized, index));
			}
		}

		public void RemoveRange(int index, int count)
		{
			if (count <= 0)
				return;

			List<T> removedItems;
			lock (_syncRoot)
			{
				CheckReentrancy();
				if (index < 0 || index >= Items.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				if (index + count > Items.Count)
					throw new ArgumentOutOfRangeException(nameof(count));

				removedItems = [];
				for (var i = 0; i < count; i++)
				{
					removedItems.Add(Items[index]);
					Items.RemoveAt(index);
				}

				OnCountPropertyChanged();
				OnIndexerPropertyChanged();
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, index));
			}
		}

		public void ReplaceRange(int index, IEnumerable<T> items)
		{
			ArgumentNullException.ThrowIfNull(items);
			var replacementItems = Materialize(items);
			if (replacementItems.Count == 0)
				return;

			List<T> oldItems;
			lock (_syncRoot)
			{
				CheckReentrancy();
				if (index < 0 || index >= Items.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				if (index + replacementItems.Count > Items.Count)
					throw new ArgumentOutOfRangeException(nameof(items));

				oldItems = [];
				for (var i = 0; i < replacementItems.Count; i++)
				{
					oldItems.Add(Items[index + i]);
					Items[index + i] = replacementItems[i];
				}

				OnIndexerPropertyChanged();
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, replacementItems, oldItems, index));
			}
		}

		protected override void ClearItems()
		{
			lock (_syncRoot)
			{
				base.ClearItems();
			}
		}

		protected override void InsertItem(int index, T item)
		{
			lock (_syncRoot)
			{
				base.InsertItem(index, item);
			}
		}

		protected override void MoveItem(int oldIndex, int newIndex)
		{
			lock (_syncRoot)
			{
				base.MoveItem(oldIndex, newIndex);
			}
		}

		protected override void RemoveItem(int index)
		{
			lock (_syncRoot)
			{
				base.RemoveItem(index);
			}
		}

		protected override void SetItem(int index, T item)
		{
			lock (_syncRoot)
			{
				base.SetItem(index, item);
			}
		}

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (_deferLevel > 0)
			{
				_hasDeferredChanges = true;
				return;
			}

			base.OnCollectionChanged(e);
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (_deferLevel > 0)
			{
				_hasDeferredChanges = true;
				return;
			}

			base.OnPropertyChanged(e);
		}

		private void RaiseReset()
		{
			lock (_syncRoot)
			{
				OnCountPropertyChanged();
				OnIndexerPropertyChanged();
				base.OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
			}
		}

		private static List<T> Materialize(IEnumerable<T> items)
		{
			if (items is List<T> list)
				return [.. list];

			return [.. items];
		}

		private void OnCountPropertyChanged()
		{
			OnPropertyChanged(EventArgsCache.CountPropertyChanged);
		}

		private void OnIndexerPropertyChanged()
		{
			OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
		}

		private sealed class BulkOperation : IDisposable
		{
			private BulkConcurrentObservableCollection<T>? _owner;

			public BulkOperation(BulkConcurrentObservableCollection<T> owner)
			{
				_owner = owner;
			}

			public void Dispose()
			{
				Interlocked.Exchange(ref _owner, null)?.EndBulkOperation();
			}
		}

		private static class EventArgsCache
		{
			internal static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");
			internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new("Item[]");
			internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new(NotifyCollectionChangedAction.Reset);
		}
	}
}
