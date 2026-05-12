// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;
using System.IO;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls
{
	public sealed partial class TreeViewSidebar : UserControl
	{
		// Lazy DI resolution — eager field-init resolves singletons during MainPage XAML parsing, before MainPage's constructor body has finished. That ordering caused process-level crashes.
		private readonly Lazy<SidebarViewModel> _sidebarViewModel = new(() => Ioc.Default.GetRequiredService<SidebarViewModel>());
		private readonly Lazy<IIconCacheService> _iconCacheService = new(() => Ioc.Default.GetRequiredService<IIconCacheService>());
		private readonly Lazy<IContentPageContext> _contentPageContext = new(() => Ioc.Default.GetRequiredService<IContentPageContext>());
		private readonly Lazy<MainPageViewModel> _mainPageViewModel = new(() => Ioc.Default.GetRequiredService<MainPageViewModel>());

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
					if (_sidebarViewModel.Value.SidebarItems is INotifyCollectionChanged inc)
						inc.CollectionChanged += OnSidebarItemsChanged;
					_currentTab = _mainPageViewModel.Value.SelectedTabItem;
					RebuildAndApply();
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
				if (_sidebarViewModel.IsValueCreated && _sidebarViewModel.Value.SidebarItems is INotifyCollectionChanged inc)
					inc.CollectionChanged -= OnSidebarItemsChanged;
				DetachAllNodeHandlers();
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
			RootFolders.Clear();

			if (_sidebarViewModel.Value.SidebarItems is not IEnumerable<INavigationControlItem> headers)
				return;

			foreach (var item in headers)
			{
				if (item is not LocationItem header || !header.IsHeader || header.Section == SectionType.Home)
					continue;

				var section = new FolderNode($"section:{header.Section}", header.Text, FolderNodeKind.Section, icon: null);

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
						var hasRootedPath = (child is LocationItem loc2 && System.IO.Path.IsPathRooted(loc2.Path)) || child is DriveItem;
						var isExpandable = hasRootedPath && header.Section != SectionType.Pinned;
						var kind = isExpandable ? FolderNodeKind.Folder : FolderNodeKind.Leaf;
						var node = new FolderNode(path, name, kind, icon);
						if (isExpandable)
							node.HasUnrealizedChildren = SafeHasSubdirectories(path);
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
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "TreeViewSidebar: rebuild failed");
			}
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

		private void Tree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
		{
			try
			{
				if (args.Node.Content is not FolderNode fn)
					return;
				LoadChildrenSync(fn);
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "TreeViewSidebar: lazy expand failed");
			}
		}

		// Synchronous lazy load. Called from Tree_Expanding (user click) and ApplyExpansionRecursive (state restore).
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
				};
				child.PropertyChanged += OnNodePropertyChanged;
				_ = LoadIconAsync(child);
				parent.Children.Add(child);
			}
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

		private void Tree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
		{
			if (args.InvokedItem is not FolderNode fn)
				return;
			if (fn.IsSection)
			{
				fn.IsExpanded = !fn.IsExpanded;
				return;
			}
			if (_contentPageContext.Value.ShellPage is { } page)
				page.NavigateToPath(fn.Path);
		}

		private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (sender is not FrameworkElement el || el.Tag is not FolderNode fn || fn.IsSection)
				return;

			var flyout = new MenuFlyout();

			var open = new MenuFlyoutItem { Text = "Open" };
			open.Click += (_, _) => _contentPageContext.Value.ShellPage?.NavigateToPath(fn.Path);
			flyout.Items.Add(open);

			var openNewTab = new MenuFlyoutItem { Text = "Open in new tab" };
			openNewTab.Click += async (_, _) => await NavigationHelpers.OpenPathInNewTab(fn.Path, true);
			flyout.Items.Add(openNewTab);

			var copyPath = new MenuFlyoutItem { Text = "Copy path" };
			copyPath.Click += (_, _) =>
			{
				var dp = new DataPackage();
				dp.SetText(fn.Path);
				Clipboard.SetContent(dp);
			};
			flyout.Items.Add(copyPath);

			flyout.Items.Add(new MenuFlyoutSeparator());

			var openExplorer = new MenuFlyoutItem { Text = "Open in File Explorer" };
			openExplorer.Click += (_, _) =>
			{
				// Win32Exception from Process.Start when the shell launch fails (invalid path or denied access)
				try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = fn.Path, UseShellExecute = true }); }
				catch (System.ComponentModel.Win32Exception) { }
			};
			flyout.Items.Add(openExplorer);

			flyout.ShowAt(el, new FlyoutShowOptions { Position = e.GetPosition(el) });
			e.Handled = true;
		}

		private static IEnumerable<string> SafeEnumerateSubdirectories(string path)
		{
			// UnauthorizedAccessException for protected folders; IOException for unavailable drives (empty optical drive, disconnected network)
			IEnumerable<string>? results = null;
			try
			{
				results = Directory.EnumerateDirectories(path)
					.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
					.Take(1000)
					.ToList();
			}
			catch (UnauthorizedAccessException) { }
			catch (IOException) { }
			return results ?? Enumerable.Empty<string>();
		}

		private static bool SafeHasSubdirectories(string path)
		{
			// Same exception conditions as SafeEnumerateSubdirectories — we just need yes/no for the disclosure indicator
			try { return Directory.EnumerateDirectories(path).Any(); }
			catch (UnauthorizedAccessException) { return false; }
			catch (IOException) { return false; }
		}
	}
}
