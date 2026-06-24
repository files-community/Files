// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using System.Collections.Specialized;

namespace Files.App.ViewModels.UserControls
{
	public sealed partial class SidebarViewModel
	{
		// Each tab is the sole source of truth for its own expansion (sections and folders alike); Pinned/Drives/CloudDrives are seeded as expanded so a fresh tab matches the historical defaults.
		private static readonly string[] SectionKeyByEnum = BuildSectionKeyByEnum();
		private static readonly string[] DefaultExpandedSectionKeys =
		{
			SectionKeyByEnum[(int)SectionType.Pinned],
			SectionKeyByEnum[(int)SectionType.Drives],
			SectionKeyByEnum[(int)SectionType.CloudDrives],
		};

		private static string[] BuildSectionKeyByEnum()
		{
			var values = Enum.GetValues<SectionType>();
			var arr = new string[(int)values.Max() + 1];
			foreach (var v in values)
				arr[(int)v] = "section:" + v;
			return arr;
		}

		private readonly Dictionary<TabBarItem, HashSet<string>> tabExpansionState = new();
		private readonly HashSet<INotifyPropertyChanged> trackedItems = new();
		private readonly HashSet<INotifyCollectionChanged> trackedChildCollections = new();
		private TabBarItem? currentTab;
		// Int counter (used with Interlocked) instead of a bool so a nested apply (e.g. TrackedChildren firing during a parent apply) can't reset the outer guard.
		private int applyingTabStateDepth;
		private bool tabTrackingInitialized;
		private MainPageViewModel? mainPageViewModel;
		private MainPageViewModel ResolveMainPageViewModel() => mainPageViewModel ??= Ioc.Default.GetRequiredService<MainPageViewModel>();

		private bool IsApplyingTabState => applyingTabStateDepth > 0;

		private static string? GetExpansionKey(INavigationControlItem item) => item switch
		{
			LocationItem { IsHeader: true } sect => SectionKeyByEnum[(int)sect.Section],
			_ when !string.IsNullOrEmpty(item.Path) => item.Path,
			_ => null,
		};

		// Defers MainPageViewModel resolution until the sidebar is hooked into MainPage to avoid DI bootstrap ordering issues.
		public void EnsureTabExpansionTrackingInitialized()
		{
			if (tabTrackingInitialized)
				return;
			tabTrackingInitialized = true;
			try
			{
				var mpvm = ResolveMainPageViewModel();
				mpvm.PropertyChanged += MainPageViewModel_PropertyChanged;
				MainPageViewModel.AppInstances.CollectionChanged += AppInstances_CollectionChanged;
				currentTab = mpvm.SelectedTabItem;
				foreach (var sectionObj in sidebarItems)
					TrackSidebarItem(sectionObj);
				ApplyTabExpansionState();
			}
			// InvalidOperationException from Ioc.Default.GetRequiredService when MainPageViewModel isn't registered yet (sidebar Loaded firing before DI bootstrap completes); clear the flag so a later Loaded retries.
			catch (InvalidOperationException)
			{
				tabTrackingInitialized = false;
			}
		}

		private void TeardownTabExpansionTracking()
		{
			if (!tabTrackingInitialized)
				return;
			if (mainPageViewModel is not null)
				mainPageViewModel.PropertyChanged -= MainPageViewModel_PropertyChanged;
			MainPageViewModel.AppInstances.CollectionChanged -= AppInstances_CollectionChanged;
			foreach (var item in trackedItems)
				item.PropertyChanged -= TrackedItem_PropertyChanged;
			foreach (var col in trackedChildCollections)
				col.CollectionChanged -= TrackedChildren_CollectionChanged;
			trackedItems.Clear();
			trackedChildCollections.Clear();
		}

		private void MainPageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(MainPageViewModel.SelectedTabItem))
				return;
			var newTab = ResolveMainPageViewModel().SelectedTabItem;
			if (ReferenceEquals(newTab, currentTab))
				return;
			currentTab = newTab;
			ApplyTabExpansionState();
		}

		private void AppInstances_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			// Treat Replace identically to Remove so the replaced tab's state isn't kept under a dead key. Move never changes membership, so its OldItems are skipped.
			if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace && e.OldItems is not null)
			{
				foreach (var raw in e.OldItems)
					if (raw is TabBarItem item)
						tabExpansionState.Remove(item);
			}
			else if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				tabExpansionState.Clear();
			}
		}

		private HashSet<string> GetOrInitTabState(TabBarItem tab)
		{
			if (!tabExpansionState.TryGetValue(tab, out var state))
			{
				state = new HashSet<string>(DefaultExpandedSectionKeys, StringComparer.OrdinalIgnoreCase);
				// Inherit whatever section state the user currently sees so a freshly opened tab matches expectation rather than snapping back to defaults.
				foreach (var sectionObj in sidebarItems)
				{
					if (sectionObj is LocationItem section && section.IsExpanded)
					{
						var key = GetExpansionKey(section);
						if (key is not null)
							state.Add(key);
					}
				}
				tabExpansionState[tab] = state;
			}
			return state;
		}

		internal void TrackSidebarItem(INavigationControlItem? item)
		{
			if (item is null || item is not INotifyPropertyChanged inpc)
				return;
			if (!trackedItems.Add(inpc))
				return;
			inpc.PropertyChanged += TrackedItem_PropertyChanged;
			HookChildrenCollection(item);
		}

		// Symmetric counterpart to TrackSidebarItem: unsubscribes the item, drops it from the tracker HashSets, removes its key from every tab's expansion state, and recurses into already-loaded descendants so a watcher-driven prune doesn't strand subscriptions or stale keys.
		internal void UntrackSidebarItem(INavigationControlItem? item)
		{
			if (item is null)
				return;
			if (item is INotifyPropertyChanged inpc && trackedItems.Remove(inpc))
				inpc.PropertyChanged -= TrackedItem_PropertyChanged;
			if (item.Children is INotifyCollectionChanged children && trackedChildCollections.Remove(children))
				children.CollectionChanged -= TrackedChildren_CollectionChanged;
			var key = GetExpansionKey(item);
			if (key is not null)
			{
				foreach (var state in tabExpansionState.Values)
					state.Remove(key);
			}
			if (item.Children is IEnumerable<INavigationControlItem> existing)
			{
				foreach (var child in existing)
					UntrackSidebarItem(child);
			}
		}

		// Runs both on initial track and when IsExpandableFolder flips true (the children collection materializes lazily). Reads via the Children property (not the raw ChildItems field) so subfolder LocationItems get their backing collection lazy-initialized and hooked — otherwise CollectionChanged fires later with no listener and deeper descendants are never tracked.
		private void HookChildrenCollection(INavigationControlItem item)
		{
			if (item.Children is not INotifyCollectionChanged children)
				return;
			if (!trackedChildCollections.Add(children))
				return;
			children.CollectionChanged += TrackedChildren_CollectionChanged;
			if (children is IEnumerable<INavigationControlItem> existing)
			{
				foreach (var child in existing)
					TrackSidebarItem(child);
			}
		}

		private void TrackedItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is not INavigationControlItem item)
				return;
			switch (e.PropertyName)
			{
				case nameof(LocationItem.IsExpanded):
					CaptureExpansionToCurrentTab(item);
					break;
				case nameof(LocationItem.IsExpandableFolder):
					HookChildrenCollection(item);
					break;
			}
		}

		private void TrackedChildren_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			// Drop subscriptions + tab-state keys for items leaving the collection so a long-running session with frequent filesystem churn doesn't grow the tracker HashSets indefinitely.
			if (e.OldItems is not null)
			{
				foreach (var raw in e.OldItems)
					if (raw is INavigationControlItem removed)
						UntrackSidebarItem(removed);
			}
			if (e.NewItems is null)
				return;
			foreach (var raw in e.NewItems)
			{
				if (raw is not INavigationControlItem added)
					continue;
				TrackSidebarItem(added);
				if (currentTab is null)
					continue;
				var state = GetOrInitTabState(currentTab);
				var key = GetExpansionKey(added);
				if (key is null || !state.Contains(key))
					continue;
				Interlocked.Increment(ref applyingTabStateDepth);
				try { added.IsExpanded = true; }
				finally { Interlocked.Decrement(ref applyingTabStateDepth); }
			}
		}

		private void CaptureExpansionToCurrentTab(INavigationControlItem item)
		{
			if (IsApplyingTabState || currentTab is null)
				return;
			var key = GetExpansionKey(item);
			if (key is null)
				return;
			var state = GetOrInitTabState(currentTab);
			if (item.IsExpanded)
				state.Add(key);
			else
				state.Remove(key);
		}

		// Restores expansion for the initial pass over sidebarItems and for items realized later (async section sync, lazy-loaded subfolders).
		private void ApplyTabExpansionState() => ApplyTabStateTo(sidebarItems);
		private void ApplyTabStateToNewlyAddedItem(INavigationControlItem item) => ApplyTabStateTo([item]);

		private void ApplyTabStateTo(IEnumerable<INavigationControlItem> items)
		{
			if (currentTab is null)
				return;
			var state = GetOrInitTabState(currentTab);
			Interlocked.Increment(ref applyingTabStateDepth);
			try
			{
				foreach (var item in items)
					ApplyStateToItem(item, state);
			}
			finally
			{
				Interlocked.Decrement(ref applyingTabStateDepth);
			}
		}

		private void ApplyStateToItem(INavigationControlItem item, HashSet<string> state)
		{
			var key = GetExpansionKey(item);
			var shouldExpand = key is not null && state.Contains(key);
			item.IsExpanded = shouldExpand;
			if (!shouldExpand || GetChildren(item) is not { } children)
				return;
			foreach (var child in children)
				ApplyStateToItem(child, state);
		}
	}
}
