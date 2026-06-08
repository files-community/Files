// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml;
using System.Collections.Specialized;

namespace Files.App.ViewModels.UserControls
{
	public sealed partial class SidebarViewModel
	{
		private BulkConcurrentObservableCollection<FlatSidebarItem>? _flatSidebarItems;
		public BulkConcurrentObservableCollection<FlatSidebarItem> FlatSidebarItems
		{
			get
			{
				if (_flatSidebarItems is null)
					InitializeFlatTree();
				return _flatSidebarItems!;
			}
		}

		// SidebarDisplayMode rejects Minimal (it only tracks user preference), so the flat tree mirrors the SidebarView's live mode separately to know when sub-rows should hide.
		private SidebarDisplayMode _actualDisplayMode;
		public SidebarDisplayMode ActualDisplayMode
		{
			get => _actualDisplayMode;
			set => SetProperty(ref _actualDisplayMode, value);
		}

		private bool IsCompactDisplayMode => ActualDisplayMode == SidebarDisplayMode.Compact;

		private readonly Dictionary<ISidebarItemModel, FlatSidebarItem> _flatLookup = [];
		private readonly Dictionary<INotifyCollectionChanged, ISidebarItemModel> _flatChildCollectionParents = [];
		// Parallel reverse index so ResubscribeChildren resolves the prior collection in O(1) instead of scanning _flatChildCollectionParents.
		private readonly Dictionary<ISidebarItemModel, INotifyCollectionChanged> _flatChildCollectionByItem = [];

		private void InitializeFlatTree()
		{
			_flatSidebarItems = [];
			PropertyChanged += FlatTree_VMPropertyChanged;
			RebuildFlatTree();
			sidebarItems.CollectionChanged += FlatTree_SidebarItemsChanged;
		}

		private void CollectVisibleSubtree(ISidebarItemModel item, int depth, List<FlatSidebarItem> sink)
		{
			if (depth > 0 && IsCompactDisplayMode)
				return;
			var rowOpacity = item is LocationItem { IsHidden: true } ? Constants.UI.DimItemOpacity : 1.0;
			sink.Add(new FlatSidebarItem(item, depth, rowOpacity));
			if (!item.IsExpanded)
				return;
			foreach (var child in EnumerateChildren(item))
				CollectVisibleSubtree(child, depth + 1, sink);
		}

		private static IEnumerable<ISidebarItemModel> EnumerateChildren(ISidebarItemModel item)
			=> item.Children as IEnumerable<ISidebarItemModel> ?? Array.Empty<ISidebarItemModel>();

		private void RegisterNodes(IEnumerable<FlatSidebarItem> nodes)
		{
			foreach (var node in nodes)
			{
				_flatLookup[node.Item] = node;
				SubscribeFlatItem(node.Item);
			}
		}

		private void RebuildFlatTree()
		{
			if (_flatSidebarItems is null)
				return;
			_flatSidebarItems.BeginBulkOperation();
			try
			{
				foreach (var node in _flatSidebarItems)
					UnsubscribeFlatItem(node.Item);
				_flatSidebarItems.Clear();
				_flatLookup.Clear();
				_flatChildCollectionByItem.Clear();
				var batch = new List<FlatSidebarItem>();
				foreach (var section in sidebarItems)
					CollectVisibleSubtree(section, 0, batch);
				RegisterNodes(batch);
				_flatSidebarItems.AddRange(batch);
			}
			finally
			{
				_flatSidebarItems.EndBulkOperation();
			}
			UpdateSectionPredecessorFlags();
			RefreshSelectionForCurrentPath();
		}

		private void FlatTree_VMPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ActualDisplayMode))
				RebuildFlatTree();
		}

		private void FlatTree_SidebarItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (dispatcherQueue is null)
				return;
			dispatcherQueue.EnqueueOrInvokeAsync(() => HandleSidebarItemsChangedAsync(e));
		}

		private async Task HandleSidebarItemsChangedAsync(NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					int insertIndex = e.NewStartingIndex >= 0 ? FlatIndexOfSection(e.NewStartingIndex) : FlatSidebarItems.Count;
					await BuildAndInsertChildrenAsync(insertIndex, CastModels(e.NewItems!), 0);
					break;
				case NotifyCollectionChangedAction.Remove:
					RemoveSubtrees(e.OldItems!);
					break;
				default:
					RebuildFlatTree();
					return;
			}
			UpdateSectionPredecessorFlags();
			RefreshSelectionForCurrentPath();
		}

		// The i-th depth=0 row in FlatSidebarItems is where section i begins, so one linear pass suffices.
		private int FlatIndexOfSection(int sectionIndex)
		{
			int seen = 0;
			for (int i = 0; i < FlatSidebarItems.Count; i++)
			{
				if (FlatSidebarItems[i].Depth != 0)
					continue;
				if (seen == sectionIndex)
					return i;
				seen++;
			}
			return FlatSidebarItems.Count;
		}

		// Walks forward from `start` and returns the index of the first row whose depth is at or above the parent's depth — i.e. the end of `start`'s subtree.
		private int FindSubtreeEnd(int start, int parentDepth)
		{
			int end = start + 1;
			while (end < FlatSidebarItems.Count && FlatSidebarItems[end].Depth > parentDepth)
				end++;
			return end;
		}

		// Unsubscribes and removes wrappers in [start, end). Caller picks whether `start` is the subtree root (full prune) or root+1 (collapse: keep the root, drop descendants).
		private void RemoveSubtreeRange(int start, int end)
		{
			int count = end - start;
			if (count <= 0)
				return;
			for (int i = start; i < end; i++)
			{
				var removed = FlatSidebarItems[i];
				UnsubscribeFlatItem(removed.Item);
				_flatLookup.Remove(removed.Item);
			}
			FlatSidebarItems.RemoveRange(start, count);
		}

		// _flatLookup gives the wrapper in O(1) but IndexOf into the observable list is still O(N); callers that need both the index and the node would otherwise repeat the miss-guard pair each time.
		private bool TryGetFlatPosition(ISidebarItemModel item, out int start, out FlatSidebarItem node)
		{
			if (_flatLookup.TryGetValue(item, out node!))
			{
				start = FlatSidebarItems.IndexOf(node);
				if (start >= 0)
					return true;
			}
			start = -1;
			node = null!;
			return false;
		}

		private void RemoveSubtrees(System.Collections.IList items)
		{
			foreach (var raw in items)
			{
				if (raw is ISidebarItemModel item && TryGetFlatPosition(item, out var start, out var node))
					RemoveSubtreeRange(start, FindSubtreeEnd(start, node.Depth));
			}
		}

		// Lazily yields ISidebarItemModel entries from a non-generic CollectionChanged list; avoids the LINQ OfType iterator allocation on the hot mutation paths.
		private static IEnumerable<ISidebarItemModel> CastModels(System.Collections.IList items)
		{
			foreach (var raw in items)
				if (raw is ISidebarItemModel item)
					yield return item;
		}

		// Collects fresh wrappers for the given children (skipping any already in the flat list) and inserts them. Returns immediately if there's nothing to add — avoids allocating the batch list in the common no-op path.
		private async Task BuildAndInsertChildrenAsync(int insertAt, IEnumerable<ISidebarItemModel> children, int childDepth)
		{
			List<FlatSidebarItem>? batch = null;
			foreach (var child in children)
			{
				if (_flatLookup.ContainsKey(child))
					continue;
				batch ??= [];
				CollectVisibleSubtree(child, childDepth, batch);
			}
			if (batch is not null)
				await InsertChunkedAsync(insertAt, batch);
		}

		private void SubscribeFlatItem(ISidebarItemModel item)
		{
			item.PropertyChanged += FlatTree_ItemPropertyChanged;
			if (item.Children is INotifyCollectionChanged notify && !_flatChildCollectionParents.ContainsKey(notify))
			{
				notify.CollectionChanged += FlatTree_ChildCollectionChanged;
				_flatChildCollectionParents[notify] = item;
				_flatChildCollectionByItem[item] = notify;
			}
		}

		private void UnsubscribeFlatItem(ISidebarItemModel item)
		{
			item.PropertyChanged -= FlatTree_ItemPropertyChanged;
			if (item.Children is INotifyCollectionChanged notify && _flatChildCollectionParents.Remove(notify))
			{
				_flatChildCollectionByItem.Remove(item);
				notify.CollectionChanged -= FlatTree_ChildCollectionChanged;
			}
		}

		// IsExpandableFolder flips asynchronously on DriveItem/LocationItem after the item enters the flat tree, swapping Children from null to a real collection — re-subscribe to the new reference.
		private void ResubscribeChildren(ISidebarItemModel item)
		{
			if (_flatChildCollectionByItem.Remove(item, out var oldCollection))
			{
				_flatChildCollectionParents.Remove(oldCollection);
				oldCollection.CollectionChanged -= FlatTree_ChildCollectionChanged;
			}
			if (item.Children is INotifyCollectionChanged newCollection && !_flatChildCollectionParents.ContainsKey(newCollection))
			{
				newCollection.CollectionChanged += FlatTree_ChildCollectionChanged;
				_flatChildCollectionParents[newCollection] = item;
				_flatChildCollectionByItem[item] = newCollection;
			}
		}

		private void FlatTree_ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is not ISidebarItemModel item)
				return;

			if (e.PropertyName == nameof(ISidebarItemModel.Children) && _flatLookup.ContainsKey(item))
				ResubscribeChildren(item);

			if (e.PropertyName == nameof(ISidebarItemModel.IsExpanded) && dispatcherQueue is not null)
				dispatcherQueue.EnqueueOrInvokeAsync(() => HandleItemExpansionChangedAsync(item));
		}

		private async Task HandleItemExpansionChangedAsync(ISidebarItemModel item)
		{
			if (!TryGetFlatPosition(item, out var start, out var node))
				return;
			if (item.IsExpanded)
				await BuildAndInsertChildrenAsync(start + 1, EnumerateChildren(item), node.Depth + 1);
			else
				RemoveSubtreeRange(start + 1, FindSubtreeEnd(start, node.Depth));
			if (node.Depth == 0)
				UpdateSectionPredecessorFlags();
			RefreshSelectionForCurrentPath();
		}

		private void FlatTree_ChildCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (sender is not INotifyCollectionChanged notify)
				return;
			if (!_flatChildCollectionParents.TryGetValue(notify, out var parent))
				return;
			if (dispatcherQueue is null)
				return;
			dispatcherQueue.EnqueueOrInvokeAsync(async () => await HandleChildCollectionChangedAsync(parent, e));
		}

		// High-fanout folders (e.g. WinSxS) freeze the UI when ItemsRepeater receives one giant Add event. Splitting into chunks with a dispatcher yield turns it into a sequence of frame-sized layout passes.
		private const int FlatTreeInsertChunkSize = 100;

		private async Task HandleChildCollectionChangedAsync(ISidebarItemModel parent, NotifyCollectionChangedEventArgs e)
		{
			if (!parent.IsExpanded || !TryGetFlatPosition(parent, out var parentStart, out var parentNode))
				return;
			int childDepth = parentNode.Depth + 1;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					await BuildAndInsertChildrenAsync(
						FindChildInsertionIndex(parentStart, childDepth, e.NewStartingIndex),
						CastModels(e.NewItems!),
						childDepth);
					break;
				case NotifyCollectionChangedAction.Remove:
					RemoveSubtrees(e.OldItems!);
					break;
				default:
					// Reset / Move / Replace: rebuild the subtree from the parent's current children.
					RemoveSubtreeRange(parentStart + 1, FindSubtreeEnd(parentStart, parentNode.Depth));
					await BuildAndInsertChildrenAsync(parentStart + 1, EnumerateChildren(parent), childDepth);
					break;
			}
			UpdateSectionPredecessorFlags();
			RefreshSelectionForCurrentPath();
		}

		private async Task InsertChunkedAsync(int insertAt, List<FlatSidebarItem> batch)
		{
			if (batch.Count == 0)
				return;
			if (batch.Count <= FlatTreeInsertChunkSize)
			{
				RegisterNodes(batch);
				FlatSidebarItems.InsertRange(insertAt, batch);
				return;
			}
			var currentInsertAt = insertAt;
			for (int i = 0; i < batch.Count; i += FlatTreeInsertChunkSize)
			{
				var chunkEnd = Math.Min(i + FlatTreeInsertChunkSize, batch.Count);
				var chunk = batch.GetRange(i, chunkEnd - i);
				RegisterNodes(chunk);
				FlatSidebarItems.InsertRange(currentInsertAt, chunk);
				if (chunkEnd < batch.Count)
				{
					await Task.Delay(1);
					currentInsertAt = FlatSidebarItems.IndexOf(batch[chunkEnd - 1]) + 1;
					if (currentInsertAt <= 0)
						return;
				}
			}
		}

		private void RefreshSelectionForCurrentPath()
		{
			if (!string.IsNullOrEmpty(CurrentPath))
				UpdateSidebarSelectedItemFromArgs(CurrentPath);
		}

		// Inter-section gap (12px top margin) only appears after an expanded section. Home has no Children and counts as "expanded" so the first section still gets a gap.
		private void UpdateSectionPredecessorFlags()
		{
			if (_flatSidebarItems is null)
				return;
			bool prevWasExpanded = false;
			foreach (var node in _flatSidebarItems)
			{
				if (node.Depth != 0)
					continue;
				node.HasExpandedPredecessor = !IsCompactDisplayMode && prevWasExpanded;
				prevWasExpanded = node.Item.Children is null ? true : node.Item.IsExpanded;
			}
		}

		private int FindChildInsertionIndex(int parentStart, int childDepth, int sourceIndex)
		{
			int parentDepth = childDepth - 1;
			int countSeen = 0;
			int i = parentStart + 1;
			while (i < FlatSidebarItems.Count && FlatSidebarItems[i].Depth > parentDepth)
			{
				if (FlatSidebarItems[i].Depth == childDepth)
				{
					if (sourceIndex >= 0 && countSeen == sourceIndex)
						return i;
					countSeen++;
				}
				i++;
			}
			return i;
		}
	}
}
