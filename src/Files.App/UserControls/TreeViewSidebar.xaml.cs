// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;
using System.IO;

namespace Files.App.UserControls
{
	public sealed partial class TreeViewSidebar : UserControl
	{
		// Lazy DI resolution — eager field-init resolves singletons during MainPage XAML parsing, before MainPage's constructor body has finished. That ordering caused process-level crashes.
		private readonly Lazy<SidebarViewModel> _sidebarViewModel = new(() => Ioc.Default.GetRequiredService<SidebarViewModel>());
		private readonly Lazy<IIconCacheService> _iconCacheService = new(() => Ioc.Default.GetRequiredService<IIconCacheService>());
		private readonly Lazy<IContentPageContext> _contentPageContext = new(() => Ioc.Default.GetRequiredService<IContentPageContext>());
		private readonly Lazy<MainPageViewModel> _mainPageViewModel = new(() => Ioc.Default.GetRequiredService<MainPageViewModel>());
		private readonly Lazy<IUserSettingsService> _userSettingsService = new(() => Ioc.Default.GetRequiredService<IUserSettingsService>());

		public ObservableCollection<FolderNode> RootFolders { get; } = new();

		// Per-tab set of expanded node IDs (section ID like "section:Pinned" OR a folder path like "C:\Users"). Seeded with default expanded sections for new tabs.
		private readonly Dictionary<TabBarItem, HashSet<string>> _tabExpansionState = new();
		private static readonly string[] DefaultExpandedSectionIds =
		{
			"section:Pinned",
			"section:Drives",
			"section:CloudDrives",
		};

		private TabBarItem? _currentTab;
		private bool _isUnloaded;
		private bool _applyingState;

		// Section.ChildItems collections we've subscribed to so dynamic sections (libraries, cloud drives, file tags) refresh when their async population completes. Tracked so we can detach before a rebuild without leaking handlers.
		private readonly List<INotifyCollectionChanged> _subscribedChildCollections = new();

		// Currently-selected node per the path mirror — kept so we can clear its IsSelected before flipping the new one without walking the whole tree.
		private FolderNode? _selectedNode;

		public TreeViewSidebar()
		{
			InitializeComponent();

			// TreeView's internal control template falls through to {Binding Children} on its inherited DataContext when our binding hasn't fully primed it. From MainPage, that's MainPageViewModel (no Children property) — leaving inheritance in place was the source of native AccessViolation crashes.
			Tree.DataContext = null;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				try
				{
					if (_isUnloaded)
						return;
					_mainPageViewModel.Value.PropertyChanged += OnMainPageViewModelPropertyChanged;
					_sidebarViewModel.Value.PropertyChanged += OnSidebarViewModelPropertyChanged;
					if (_sidebarViewModel.Value.SidebarItems is INotifyCollectionChanged inc)
						inc.CollectionChanged += OnSidebarItemsChanged;
					MainPageViewModel.AppInstances.CollectionChanged += OnAppInstancesChanged;
					_currentTab = _mainPageViewModel.Value.SelectedTabItem;
					RebuildAndApply();
					UpdateSelectionFromCurrentPath();
				}
				catch (Exception ex)
				{
					App.Logger?.LogWarning(ex, "TreeViewSidebar: Loaded init failed");
				}
			});
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_isUnloaded = true;
			try
			{
				if (_mainPageViewModel.IsValueCreated)
					_mainPageViewModel.Value.PropertyChanged -= OnMainPageViewModelPropertyChanged;
				if (_sidebarViewModel.IsValueCreated)
				{
					_sidebarViewModel.Value.PropertyChanged -= OnSidebarViewModelPropertyChanged;
					if (_sidebarViewModel.Value.SidebarItems is INotifyCollectionChanged inc)
						inc.CollectionChanged -= OnSidebarItemsChanged;
				}
				MainPageViewModel.AppInstances.CollectionChanged -= OnAppInstancesChanged;
				DetachAllNodeHandlers();
				DetachChildCollectionHandlers();
			}
			// Cleanup must never throw; the page is being torn down
			catch (Exception ex)
			{
				App.Logger?.LogDebug(ex, "TreeViewSidebar: Unloaded cleanup failed");
			}
		}

		private void DetachAllNodeHandlers()
		{
			foreach (var section in RootFolders)
				DetachRecursive(section);
		}

		private void DetachRecursive(FolderNode node)
		{
			node.PropertyChanged -= OnNodePropertyChanged;
			foreach (var child in node.Children)
				DetachRecursive(child);
		}

		// Snapshot the sidebar sections + their direct child items. Drive subfolders are loaded lazily via Tree_Expanding or via state-driven expansion in ApplyTabState.
		private void Rebuild()
		{
			DetachAllNodeHandlers();
			DetachChildCollectionHandlers();
			// The old _selectedNode is about to be replaced by fresh FolderNode instances — drop it so UpdateSelectionFromCurrentPath re-finds the match against the new tree.
			_selectedNode = null;
			RootFolders.Clear();

			if (_sidebarViewModel.Value.SidebarItems is not IEnumerable<INavigationControlItem> headers)
				return;

			foreach (var item in headers)
			{
				if (item is not LocationItem header || !header.IsHeader)
					continue;

				if (header.Section == SectionType.Home)
				{
					var homeNode = new FolderNode(header.Path, header.Text, FolderNodeKind.Leaf, header.Icon) { SourceItem = header };
					homeNode.PropertyChanged += OnNodePropertyChanged;
					RootFolders.Add(homeNode);
					continue;
				}

				var section = new FolderNode($"section:{header.Section}", header.Text, FolderNodeKind.Section, icon: null) { SourceItem = header };

				// Libraries, cloud drives, and file tags populate async — their ChildItems collection emits its own change events that SidebarItems doesn't propagate.
				if (header.ChildItems is INotifyCollectionChanged childIncc)
				{
					childIncc.CollectionChanged += OnSectionChildItemsChanged;
					_subscribedChildCollections.Add(childIncc);
				}

				if (header.ChildItems is not null)
				{
					foreach (var childObj in header.ChildItems)
					{
						if (childObj is not INavigationControlItem child)
							continue;
						var path = child.Path;
						var name = child.Text;
						if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name))
							continue;
						var icon = child switch
						{
							LocationItem loc => loc.Icon,
							DriveItem drv => drv.Icon,
							_ => null,
						};
						var hasRootedPath = (child is LocationItem loc2 && Path.IsPathRooted(loc2.Path)) || child is DriveItem;
						var isExpandable = hasRootedPath && header.Section != SectionType.Pinned;
						var kind = isExpandable ? FolderNodeKind.Folder : FolderNodeKind.Leaf;
						var node = new FolderNode(path, name, kind, icon) { SourceItem = child };
						if (isExpandable)
							_ = ProbeHasChildrenAsync(node);
						node.PropertyChanged += OnNodePropertyChanged;
						section.Children.Add(node);
					}
				}

				section.PropertyChanged += OnNodePropertyChanged;
				RootFolders.Add(section);
			}
		}

		// Walk the tree and apply the per-tab expansion state. For folders with HasUnrealizedChildren that should be expanded, lazy-load children synchronously and recurse so deep state restores correctly.
		private void ApplyTabState()
		{
			if (_currentTab is null)
				return;
			var state = GetOrInitState(_currentTab);
			_applyingState = true;
			try
			{
				foreach (var section in RootFolders)
					ApplyExpansionRecursive(section, state);
			}
			finally
			{
				_applyingState = false;
			}
		}

		private void ApplyExpansionRecursive(FolderNode node, HashSet<string> state)
		{
			var shouldExpand = state.Contains(node.Path);
			if (shouldExpand && node.Kind == FolderNodeKind.Folder && node.HasUnrealizedChildren)
				LoadChildrenSync(node);

			node.IsExpanded = shouldExpand;

			if (shouldExpand)
			{
				foreach (var child in node.Children.ToList())
					ApplyExpansionRecursive(child, state);
			}
		}

		private void RebuildAndApply()
		{
			if (_isUnloaded)
				return;
			try
			{
				Rebuild();
				ApplyTabState();
				// Re-evaluate the selection against the fresh tree — drives plugged in, cloud sync arriving, tab change, etc.
				UpdateSelectionFromCurrentPath();
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "TreeViewSidebar: rebuild failed");
			}
		}

		// CurrentPath updates whenever the user navigates (via address bar, breadcrumbs, sidebar click, etc.). Mirror it onto the tree so the deepest already-loaded ancestor highlights.
		private void OnSidebarViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(SidebarViewModel.CurrentPath))
				return;
			if (DispatcherQueue.HasThreadAccess)
				UpdateSelectionFromCurrentPath();
			else
				DispatcherQueue.TryEnqueue(UpdateSelectionFromCurrentPath);
		}

		// Walks the tree finding the deepest already-realized ancestor of CurrentPath and flips IsSelected on it (clearing the previous selection). The DataTemplate's overlay Border paints the visual — we never touch TreeView.SelectedItem, since assigning that to a data node with an unrealized container AVs WinUI's native selection code.
		private void UpdateSelectionFromCurrentPath()
		{
			if (_isUnloaded)
				return;
			var target = _sidebarViewModel.Value.CurrentPath;
			var match = string.IsNullOrEmpty(target) ? null : FindDeepestAncestor(RootFolders, target);
			if (ReferenceEquals(match, _selectedNode))
				return;
			if (_selectedNode is not null)
				_selectedNode.IsSelected = false;
			_selectedNode = match;
			if (_selectedNode is not null)
				_selectedNode.IsSelected = true;
		}

		private static FolderNode? FindDeepestAncestor(IEnumerable<FolderNode> nodes, string targetPath)
		{
			FolderNode? best = null;
			foreach (var node in nodes)
			{
				// Section nodes use a "section:..." pseudo-path that never matches a real folder — recurse into children instead.
				if (node.IsSection)
				{
					var deeperInSection = FindDeepestAncestor(node.Children, targetPath);
					if (deeperInSection is not null && (best is null || deeperInSection.Path.Length > best.Path.Length))
						best = deeperInSection;
					continue;
				}
				if (string.IsNullOrEmpty(node.Path))
					continue;
				if (targetPath.Equals(node.Path, StringComparison.OrdinalIgnoreCase))
					return node;
				// Treat drive-style paths ending without a separator ("C:") as ancestors of "C:\..." by appending the separator before comparing.
				var withSeparator = node.Path.EndsWith(Path.DirectorySeparatorChar) ? node.Path : node.Path + Path.DirectorySeparatorChar;
				if (targetPath.StartsWith(withSeparator, StringComparison.OrdinalIgnoreCase))
				{
					var deeper = FindDeepestAncestor(node.Children, targetPath);
					var candidate = deeper ?? node;
					if (best is null || candidate.Path.Length > best.Path.Length)
						best = candidate;
				}
			}
			return best;
		}

		private HashSet<string> GetOrInitState(TabBarItem tab)
		{
			if (_tabExpansionState.TryGetValue(tab, out var state))
				return state;
			state = new HashSet<string>(DefaultExpandedSectionIds, StringComparer.OrdinalIgnoreCase);
			_tabExpansionState[tab] = state;
			return state;
		}

		// Persist any node's expansion state (sections AND lazy-loaded folders) to the current tab.
		private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (_applyingState || e.PropertyName != nameof(FolderNode.IsExpanded))
				return;
			if (sender is not FolderNode node || _currentTab is null)
				return;
			var state = GetOrInitState(_currentTab);
			if (node.IsExpanded)
				state.Add(node.Path);
			else
				state.Remove(node.Path);
		}

		private void OnSidebarItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (DispatcherQueue.HasThreadAccess)
				RebuildAndApply();
			else
				DispatcherQueue.TryEnqueue(RebuildAndApply);
		}

		private void OnSectionChildItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (DispatcherQueue.HasThreadAccess)
				RebuildAndApply();
			else
				DispatcherQueue.TryEnqueue(RebuildAndApply);
		}

		private void DetachChildCollectionHandlers()
		{
			foreach (var c in _subscribedChildCollections)
				c.CollectionChanged -= OnSectionChildItemsChanged;
			_subscribedChildCollections.Clear();
		}

		private void OnAppInstancesChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
			{
				foreach (var item in e.OldItems.OfType<TabBarItem>())
					_tabExpansionState.Remove(item);
			}
			else if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				_tabExpansionState.Clear();
			}
		}

		private void OnMainPageViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(MainPageViewModel.SelectedTabItem))
				return;
			if (DispatcherQueue.HasThreadAccess)
				HandleTabChange();
			else
				DispatcherQueue.TryEnqueue(HandleTabChange);
		}

		private void HandleTabChange()
		{
			if (_isUnloaded)
				return;
			try
			{
				var newTab = _mainPageViewModel.Value.SelectedTabItem;
				if (newTab is null || ReferenceEquals(newTab, _currentTab))
					return;
				_currentTab = newTab;
				ApplyTabState();
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "TreeViewSidebar: tab change handler failed");
			}
		}

		private async void Tree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
		{
			try
			{
				if (args.Node.Content is not FolderNode fn)
					return;
				await LoadChildrenAsync(fn);
				// The just-loaded children may contain a deeper ancestor of CurrentPath — let the highlight descend.
				UpdateSelectionFromCurrentPath();
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "TreeViewSidebar: lazy expand failed");
			}
		}

		private async Task LoadChildrenAsync(FolderNode parent)
		{
			if (parent.Kind != FolderNodeKind.Folder || !parent.HasUnrealizedChildren)
				return;

			// Mark before awaiting so a stray re-entry doesn't double-load
			parent.HasUnrealizedChildren = false;

			var parentPath = parent.Path;
			var loaded = await Task.Run(() =>
			{
				var results = new List<(string Sub, string Name, bool HasGrandchildren, bool IsHidden)>();
				foreach (var sub in SafeEnumerateSubdirectories(parentPath))
				{
					var subName = Path.GetFileName(sub);
					if (string.IsNullOrEmpty(subName))
						continue;
					results.Add((sub, subName, SafeHasSubdirectories(sub), GetIsHidden(sub)));
				}
				return results;
			});

			if (_isUnloaded)
				return;

			foreach (var (sub, subName, hasGrandchildren, isHidden) in loaded)
			{
				var kind = hasGrandchildren ? FolderNodeKind.Folder : FolderNodeKind.Leaf;
				var child = new FolderNode(sub, subName, kind, icon: null)
				{
					HasUnrealizedChildren = hasGrandchildren,
					Opacity = isHidden ? Constants.UI.DimItemOpacity : 1.0,
					SourceItem = CreateSubfolderLocationItem(sub, subName),
				};
				child.PropertyChanged += OnNodePropertyChanged;
				_ = LoadIconAsync(child);
				parent.Children.Add(child);
			}
		}

		// Synchronous lazy load. Called from ApplyExpansionRecursive (state restore) to keep tab-switch restoration synchronous.
		private void LoadChildrenSync(FolderNode parent)
		{
			if (parent.Kind != FolderNodeKind.Folder || !parent.HasUnrealizedChildren)
				return;

			// Mark before populating so a stray re-entry doesn't double-load
			parent.HasUnrealizedChildren = false;

			foreach (var sub in SafeEnumerateSubdirectories(parent.Path))
			{
				var subName = Path.GetFileName(sub);
				if (string.IsNullOrEmpty(subName))
					continue;
				var hasGrandchildren = SafeHasSubdirectories(sub);
				var kind = hasGrandchildren ? FolderNodeKind.Folder : FolderNodeKind.Leaf;
				var child = new FolderNode(sub, subName, kind, icon: null)
				{
					HasUnrealizedChildren = hasGrandchildren,
					Opacity = GetIsHidden(sub) ? Constants.UI.DimItemOpacity : 1.0,
					SourceItem = CreateSubfolderLocationItem(sub, subName),
				};
				child.PropertyChanged += OnNodePropertyChanged;
				_ = LoadIconAsync(child);
				parent.Children.Add(child);
			}
		}

		// Lazy-loaded subfolders aren't part of SidebarViewModel.SidebarItems, so synthesize a LocationItem to feed HandleItemContextInvokedAsync. Mirrors the menu options used for pinned folders, minus pin/unpin (these aren't pinned).
		private static LocationItem CreateSubfolderLocationItem(string path, string name)
		{
			return new LocationItem
			{
				Path = path,
				Text = name,
				MenuOptions = new ContextMenuOptions
				{
					IsLocationItem = true,
					ShowProperties = true,
					ShowShellItems = true,
				},
			};
		}

		private async Task LoadIconAsync(FolderNode node)
		{
			try
			{
				var bytes = await _iconCacheService.Value.GetIconAsync(node.Path, null, isFolder: true);
				if (bytes is null || _isUnloaded)
					return;
				var bmp = await bytes.ToBitmapAsync();
				if (bmp is not null && !_isUnloaded)
					node.Icon = bmp;
			}
			// Icon resolution can fail for inaccessible paths (network down, missing folder) — leave icon null
			catch (Exception ex)
			{
				App.Logger?.LogDebug(ex, "TreeViewSidebar: icon load failed for {Path}", node.Path);
			}
		}

		private async Task ProbeHasChildrenAsync(FolderNode node)
		{
			var path = node.Path;
			var hasChildren = await Task.Run(() => SafeHasSubdirectories(path));
			if (_isUnloaded)
				return;
			node.HasUnrealizedChildren = hasChildren;
		}

		private void Tree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
		{
			if (args.InvokedItem is not FolderNode fn)
				return;
			if (fn.IsSection)
			{
				fn.IsExpanded = !fn.IsExpanded;
				return;
			}
			if (_contentPageContext.Value.ShellPage is not { } page)
				return;
			if (string.Equals(fn.Path, "Home", StringComparison.OrdinalIgnoreCase))
				page.NavigateHome();
			else
				page.NavigateToPath(fn.Path);
		}

		private async void Item_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (sender is not FrameworkElement el || el.Tag is not FolderNode fn)
				return;
			if (fn.Kind != FolderNodeKind.Folder)
				return;
			if (!fn.IsExpanded && fn.HasUnrealizedChildren)
				await LoadChildrenAsync(fn);
			fn.IsExpanded = !fn.IsExpanded;
			e.Handled = true;
		}

		private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (sender is not FrameworkElement el || el.Tag is not FolderNode fn || fn.SourceItem is null)
				return;

			_sidebarViewModel.Value.HandleItemContextInvokedAsync(el, new ItemContextInvokedArgs(fn.SourceItem, e.GetPosition(el)));
			e.Handled = true;
		}

		private IEnumerable<string> SafeEnumerateSubdirectories(string path)
		{
			// UnauthorizedAccessException for protected folders; IOException for unavailable drives (empty optical drive, disconnected network)
			IEnumerable<string>? results = null;
			try
			{
				var folders = _userSettingsService.Value.FoldersSettingsService;
				var showHidden = folders.ShowHiddenItems;
				var showProtected = folders.ShowProtectedSystemFiles;
				var showDot = folders.ShowDotFiles;
				results = Directory.EnumerateDirectories(path)
					.Where(p => IsVisible(p, showHidden, showProtected, showDot))
					.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
					.Take(1000)
					.ToList();
			}
			catch (UnauthorizedAccessException) { }
			catch (IOException) { }
			return results ?? Enumerable.Empty<string>();
		}

		private bool SafeHasSubdirectories(string path)
		{
			// Same exception conditions as SafeEnumerateSubdirectories — we just need yes/no for the disclosure indicator
			try
			{
				var folders = _userSettingsService.Value.FoldersSettingsService;
				var showHidden = folders.ShowHiddenItems;
				var showProtected = folders.ShowProtectedSystemFiles;
				var showDot = folders.ShowDotFiles;
				return Directory.EnumerateDirectories(path).Any(p => IsVisible(p, showHidden, showProtected, showDot));
			}
			catch (UnauthorizedAccessException) { return false; }
			catch (IOException) { return false; }
		}

		private static bool GetIsHidden(string path)
		{
			try { return (File.GetAttributes(path) & FileAttributes.Hidden) == FileAttributes.Hidden; }
			catch (UnauthorizedAccessException) { return false; }
			catch (IOException) { return false; }
		}

		private static bool IsVisible(string path, bool showHidden, bool showProtected, bool showDot)
		{
			if (!showDot && Path.GetFileName(path) is string name && name.StartsWith('.'))
				return false;
			FileAttributes attr;
			try { attr = File.GetAttributes(path); }
			catch (UnauthorizedAccessException) { return false; }
			catch (IOException) { return false; }
			var isHidden = (attr & FileAttributes.Hidden) == FileAttributes.Hidden;
			var isSystem = (attr & FileAttributes.System) == FileAttributes.System;
			if (isHidden && !showHidden)
				return false;
			if (isHidden && isSystem && !showProtected)
				return false;
			return true;
		}
	}
}
