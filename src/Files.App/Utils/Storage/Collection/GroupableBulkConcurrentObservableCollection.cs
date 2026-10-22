// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Collections.Specialized;

namespace Files.App.Utils.Storage
{
	[WinRT.GeneratedWinRTExposedType]
	public sealed partial class GroupableBulkConcurrentObservableCollection<T> : BulkConcurrentObservableCollection<T>
	{
		// When 'GroupOption' is set to 'None' or when a folder is opened, 'GroupedCollection' is assigned 'null' by 'ItemGroupKeySelector'
		public BulkConcurrentObservableCollection<GroupedCollection<T>>? GroupedCollection { get; private set; }

		public bool IsGrouped => ItemGroupKeySelector is not null;

		private Func<T, string?>? itemGroupKeySelector;

		public Func<T, string?>? ItemGroupKeySelector
		{
			get => itemGroupKeySelector;
			set
			{
				itemGroupKeySelector = value;
				if (value is not null)
					GroupedCollection ??= [];
				else
					GroupedCollection = null;
			}
		}

		public Action<GroupedCollection<T>>? GetGroupHeaderInfo { get; set; }
		public Action<GroupedCollection<T>>? GetExtendedGroupHeaderInfo { get; set; }

		protected override void OnBulkOperationStarted()
		{
			GroupedCollection?.ForEach(gp => gp.BeginBulkOperation());
			GroupedCollection?.BeginBulkOperation();
		}

		protected override void OnBulkOperationEnded()
		{
			GroupedCollection?.ForEach(gp => gp.EndBulkOperation());
			GroupedCollection?.EndBulkOperation();
		}

		protected override void OnCleared()
		{
			GroupedCollection?.Clear();
		}

		protected override void UpdateGroups(NotifyCollectionChangedEventArgs e)
		{
			if (IsGrouped)
			{
				if (e.NewItems is not null)
					AddItemsToGroup(e.NewItems.Cast<T>());

				if (e.OldItems is not null)
					RemoveItemsFromGroup(e.OldItems.Cast<T>());
			}
		}

		public void ResetGroups(CancellationToken token = default)
		{
			if (!IsGrouped)
				return;

			// Prevents any unwanted errors caused by bindings updating
			GroupedCollection?.ForEach(x => x.Model.PausePropertyChangedNotifications());
			GroupedCollection?.Clear();
			AddItemsToGroup(Items, token);
		}

		private void AddItemsToGroup(IEnumerable<T> items, CancellationToken token = default)
		{
			if (GroupedCollection is null)
				return;

			var groupsByKey = new Dictionary<string, GroupedCollection<T>>(StringComparer.Ordinal);
			foreach (var group in GroupedCollection)
			{
				if (group.Model.Key is not null)
					groupsByKey.TryAdd(group.Model.Key, group);
			}

			foreach (var item in items)
			{
				if (token.IsCancellationRequested)
					return;

				var key = GetGroupKeyForItem(item);
				if (key is null)
					continue;

				groupsByKey.TryGetValue(key, out var gp);
				if (item is IGroupableItem groupable)
					groupable.Key = key;

				if (gp is not null)
				{
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
					if (GetGroupHeaderInfo is not null)
						GetGroupHeaderInfo.Invoke(group);

					GroupedCollection?.Add(group);
					GroupedCollection!.IsSorted = false;
					groupsByKey[key] = group;
				}
			}
		}

		private void RemoveItemsFromGroup(IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				var key = GetGroupKeyForItem(item);

				var group = GroupedCollection?.Where(x => x.Model.Key == key).FirstOrDefault();
				if (group is not null)
				{
					group.Remove(item);
					if (group.Count == 0)
						GroupedCollection?.Remove(group);
				}
			}
		}

		/// <summary>
		/// Moves the item to its correct group if its group key has changed, e.g. after its properties were updated.
		/// </summary>
		/// <returns>True if the item was moved to another group; otherwise, false.</returns>
		public bool UpdateItemGroup(T item)
		{
			if (!IsGrouped || item is not IGroupableItem groupable)
				return false;

			var key = GetGroupKeyForItem(item);
			if (key is null || key == groupable.Key)
				return false;

			// Only move items that are currently displayed in their recorded group
			var oldGroup = GroupedCollection?.FirstOrDefault(x => x.Model.Key == groupable.Key);
			if (oldGroup is null || !oldGroup.Remove(item))
				return false;

			if (oldGroup.Count == 0)
				GroupedCollection?.Remove(oldGroup);

			AddItemsToGroup((T[])[item]);

			return true;
		}

		private string? GetGroupKeyForItem(T item)
		{
			return ItemGroupKeySelector?.Invoke(item);
		}
	}
}
