// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Files.App.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;
using System.IO;

namespace Files.App.ViewModels.UserControls
{
	public sealed partial class TreeViewSidebarViewModel : ObservableObject
	{
		// Dependency injections

		private readonly IIconCacheService IconCacheService = Ioc.Default.GetRequiredService<IIconCacheService>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly SidebarViewModel SidebarViewModel = Ioc.Default.GetRequiredService<SidebarViewModel>();
		private readonly MainPageViewModel MainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		// Fields

		private readonly DispatcherQueue _dispatcherQueue;

		private readonly Dictionary<TabBarItem, HashSet<string>> _tabExpansionState = new();

		private static readonly string[] DefaultExpandedSectionIds =
		{
			"section:Pinned",
			"section:Drives",
			"section:CloudDrives",
		};

		private readonly List<(INotifyCollectionChanged Source, NotifyCollectionChangedEventHandler Handler)> _childItemsHandlers = new();

		private TabBarItem? _currentTab;
		private bool _isActive;
		private bool _applyingState;
		private bool _rebuildPending;

		// Properties

		public ObservableCollection<FolderNode> RootFolders { get; } = new();

		private FolderNode? _selectedNode;
		public FolderNode? SelectedNode
		{
			get => _selectedNode;
			private set => SetProperty(ref _selectedNode, value);
		}

		// Constructor

		public TreeViewSidebarViewModel()
		{
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		}

		// Methods

		public void OnControlLoaded()
		{
			_isActive = true;
			SidebarViewModel.PropertyChanged += OnSidebarViewModelPropertyChanged;
			if (SidebarViewModel.SidebarItems is INotifyCollectionChanged inc)
				inc.CollectionChanged += OnSidebarItemsChanged;
			MainPageViewModel.AppInstances.CollectionChanged += OnAppInstancesChanged;
			MainPageViewModel.PropertyChanged += OnMainPageViewModelPropertyChanged;
			_currentTab = MainPageViewModel.SelectedTabItem;
			RebuildAndApply();
			UpdateSelectionFromCurrentPath();
		}

		public void OnControlUnloaded()
		{
			_isActive = false;
			SidebarViewModel.PropertyChanged -= OnSidebarViewModelPropertyChanged;
			if (SidebarViewModel.SidebarItems is INotifyCollectionChanged inc)
				inc.CollectionChanged -= OnSidebarItemsChanged;
			MainPageViewModel.AppInstances.CollectionChanged -= OnAppInstancesChanged;
			MainPageViewModel.PropertyChanged -= OnMainPageViewModelPropertyChanged;
			DetachChildItemsHandlers();
			DetachAllNodeHandlers();
		}

		private void DetachAllNodeHandlers()
		{
			foreach (var section in RootFolders)
				DetachRecursive(section);
		}

		private void DetachChildItemsHandlers()
		{
			foreach (var (source, handler) in _childItemsHandlers)
				source.CollectionChanged -= handler;
			_childItemsHandlers.Clear();
		}

		private void DetachRecursive(FolderNode node)
		{
			node.PropertyChanged -= OnNodePropertyChanged;
			if (node.SourceItem is not null)
				node.SourceItem.PropertyChanged -= OnSourceItemPropertyChanged;
			foreach (var child in node.Children)
				DetachRecursive(child);
		}

		private void Rebuild()
		{
			DetachChildItemsHandlers();
			DetachAllNodeHandlers();
			SelectedNode = null;
			RootFolders.Clear();

			if (SidebarViewModel.SidebarItems is not IEnumerable<INavigationControlItem> headers)
				return;

			foreach (var item in headers)
			{
				if (item is not LocationItem header || !header.IsHeader)
					continue;

				if (header.Section == SectionType.Home)
				{
					var homeNode = new FolderNode(header.Path, header.Text, FolderNodeKind.Leaf, GetIcon(header), header);
					homeNode.PropertyChanged += OnNodePropertyChanged;
					header.PropertyChanged += OnSourceItemPropertyChanged;
					RootFolders.Add(homeNode);
					continue;
				}

				var section = new FolderNode($"section:{header.Section}", header.Text, FolderNodeKind.Section, icon: null);

				if (header.ChildItems is not null)
				{
					NotifyCollectionChangedEventHandler childHandler = OnSectionChildItemsChanged;
					header.ChildItems.CollectionChanged += childHandler;
					_childItemsHandlers.Add((header.ChildItems, childHandler));

					foreach (var childObj in header.ChildItems)
					{
						if (childObj is not INavigationControlItem child)
							continue;
						var path = child.Path;
						var name = child.Text;
						if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name))
							continue;
						var hasRootedPath = (child is LocationItem loc2 && Path.IsPathRooted(loc2.Path)) || child is DriveItem;
						var isExpandable = hasRootedPath && header.Section != SectionType.Pinned;
						var kind = isExpandable ? FolderNodeKind.Folder : FolderNodeKind.Leaf;
						var node = new FolderNode(path, name, kind, GetIcon(child), child)
						{
							TagIconSource = GetTagIconSource(child),
						};
						if (isExpandable)
						{
							node.HasUnrealizedChildren = true;
							_ = ProbeHasChildrenAsync(node);
						}
						node.PropertyChanged += OnNodePropertyChanged;
						child.PropertyChanged += OnSourceItemPropertyChanged;
						section.Children.Add(node);
					}
				}

				section.PropertyChanged += OnNodePropertyChanged;
				RootFolders.Add(section);
			}
		}

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
			if (!_isActive)
				return;
			try
			{
				Rebuild();
				ApplyTabState();
				UpdateSelectionFromCurrentPath();
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "TreeViewSidebar: rebuild failed");
			}
		}

		private void OnSidebarViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(SidebarViewModel.CurrentPath))
				return;
			var target = SidebarViewModel.CurrentPath;
			if (_dispatcherQueue.HasThreadAccess)
				_ = ExpandToPathAsync(target);
			else
				_dispatcherQueue.TryEnqueue(() => _ = ExpandToPathAsync(target));
		}

		// Walks the tree to find the deepest already-realized ancestor of CurrentPath and sets IsSelected on it. We deliberately never touch TreeView.SelectedItem — assigning it to a node whose container isn't realized crashes WinUI's native selection machinery (ExecutionEngineException).
		public void UpdateSelectionFromCurrentPath()
		{
			if (!_isActive)
				return;
			var target = SidebarViewModel.CurrentPath;
			var match = string.IsNullOrEmpty(target) ? null : FindDeepestAncestor(RootFolders, target);
			if (ReferenceEquals(match, SelectedNode))
				return;
			if (SelectedNode is not null)
				SelectedNode.IsSelected = false;
			SelectedNode = match;
			if (SelectedNode is not null)
				SelectedNode.IsSelected = true;
		}

		private static FolderNode? FindDeepestAncestor(IEnumerable<FolderNode> nodes, string targetPath)
		{
			FolderNode? best = null;
			foreach (var node in nodes)
			{
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
				// Drive-style paths like "C:" need a separator appended before comparing against "C:\...".
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

		private async Task ExpandToPathAsync(string? targetPath)
		{
			if (string.IsNullOrEmpty(targetPath) || !_isActive)
			{
				UpdateSelectionFromCurrentPath();
				return;
			}

			FolderNode? bestMatch = null;
			foreach (var section in RootFolders.ToList())
			{
				if (!_isActive)
					return;
				if (section.IsSection)
				{
					foreach (var child in section.Children.ToList())
					{
						if (PathIsAncestorOrMatch(child.Path, targetPath))
						{
							if (bestMatch is null || child.Path.Length > bestMatch.Path.Length)
								bestMatch = child;
						}
					}
				}
				else if (PathIsAncestorOrMatch(section.Path, targetPath))
				{
					if (bestMatch is null || section.Path.Length > bestMatch.Path.Length)
						bestMatch = section;
				}
			}

			if (bestMatch is not null)
			{
				await ExpandTowardAsync(bestMatch, targetPath);
				UpdateSelectionFromCurrentPath();
				return;
			}

			UpdateSelectionFromCurrentPath();
		}

		private async Task ExpandTowardAsync(FolderNode node, string targetPath)
		{
			if (!_isActive || node.Kind != FolderNodeKind.Folder)
				return;
			if (targetPath.Equals(node.Path, StringComparison.OrdinalIgnoreCase))
				return;
			await LoadChildrenAsync(node);
			if (!node.IsExpanded)
				node.IsExpanded = true;
			foreach (var child in node.Children.ToList())
			{
				if (!_isActive)
					return;
				if (PathIsAncestorOrMatch(child.Path, targetPath))
				{
					await ExpandTowardAsync(child, targetPath);
					return;
				}
			}
		}

		private static bool PathIsAncestorOrMatch(string nodePath, string targetPath)
		{
			if (string.IsNullOrEmpty(nodePath))
				return false;
			if (targetPath.Equals(nodePath, StringComparison.OrdinalIgnoreCase))
				return true;
			var withSep = nodePath.EndsWith(Path.DirectorySeparatorChar) ? nodePath : nodePath + Path.DirectorySeparatorChar;
			return targetPath.StartsWith(withSep, StringComparison.OrdinalIgnoreCase);
		}

		private HashSet<string> GetOrInitState(TabBarItem tab)
		{
			if (_tabExpansionState.TryGetValue(tab, out var state))
				return state;
			state = new HashSet<string>(DefaultExpandedSectionIds, StringComparer.OrdinalIgnoreCase);
			_tabExpansionState[tab] = state;
			return state;
		}

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

		private void ScheduleRebuildAndApply()
		{
			if (_rebuildPending || !_isActive)
				return;
			_rebuildPending = true;
			_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
			{
				_rebuildPending = false;
				RebuildAndApply();
			});
		}

		private void OnSidebarItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (_dispatcherQueue.HasThreadAccess)
				ScheduleRebuildAndApply();
			else
				_dispatcherQueue.TryEnqueue(ScheduleRebuildAndApply);
		}

		private void OnSectionChildItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (_dispatcherQueue.HasThreadAccess)
				ScheduleRebuildAndApply();
			else
				_dispatcherQueue.TryEnqueue(ScheduleRebuildAndApply);
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
			if (_dispatcherQueue.HasThreadAccess)
				HandleTabChange();
			else
				_dispatcherQueue.TryEnqueue(HandleTabChange);
		}

		private void HandleTabChange()
		{
			if (!_isActive)
				return;
			try
			{
				var newTab = MainPageViewModel.SelectedTabItem;
				if (newTab is null || ReferenceEquals(newTab, _currentTab))
					return;
				_currentTab = newTab;
				ApplyTabState();
				UpdateSelectionFromCurrentPath();
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "TreeViewSidebar: tab change handler failed");
			}
		}

		public async Task LoadChildrenAsync(FolderNode parent)
		{
			if (parent.Kind != FolderNodeKind.Folder || !parent.HasUnrealizedChildren)
				return;

			// Mark before awaiting so a stray re-entry doesn't double-load
			parent.HasUnrealizedChildren = false;

			var folders = UserSettingsService.FoldersSettingsService;
			var showHidden = folders.ShowHiddenItems;
			var showProtected = folders.ShowProtectedSystemFiles;
			var showDot = folders.ShowDotFiles;
			var parentPath = parent.Path;

			var entries = await Task.Run(() => FolderHelpers.EnumerateSubfolders(parentPath, showHidden, showProtected, showDot));

			if (!_isActive)
				return;

			foreach (var entry in entries)
			{
				var kind = entry.HasSubfolders ? FolderNodeKind.Folder : FolderNodeKind.Leaf;
				var child = new FolderNode(entry.Path, entry.Name, kind, icon: null)
				{
					HasUnrealizedChildren = entry.HasSubfolders,
					Opacity = entry.IsHidden ? Constants.UI.DimItemOpacity : 1.0,
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

			var folders = UserSettingsService.FoldersSettingsService;
			var entries = FolderHelpers.EnumerateSubfolders(parent.Path, folders.ShowHiddenItems, folders.ShowProtectedSystemFiles, folders.ShowDotFiles);

			foreach (var entry in entries)
			{
				var kind = entry.HasSubfolders ? FolderNodeKind.Folder : FolderNodeKind.Leaf;
				var child = new FolderNode(entry.Path, entry.Name, kind, icon: null)
				{
					HasUnrealizedChildren = entry.HasSubfolders,
					Opacity = entry.IsHidden ? Constants.UI.DimItemOpacity : 1.0,
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
				var bytes = await IconCacheService.GetIconAsync(node.Path, null, isFolder: true);
				if (bytes is null || !_isActive)
					return;
				var bmp = await bytes.ToBitmapAsync();
				if (bmp is not null && _isActive)
					node.Icon = bmp;
			}
			// Icon resolution can fail for inaccessible paths (network down, missing folder) — leave icon null
			catch (Exception ex)
			{
				App.Logger?.LogDebug(ex, "TreeViewSidebar: icon load failed for {Path}", node.Path);
			}
		}

		private void OnSourceItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != "IconElement")
				return;
			if (sender is not INavigationControlItem sourceItem)
				return;
			if (_dispatcherQueue.HasThreadAccess)
				SyncIconFromSourceItem(sourceItem);
			else
				_dispatcherQueue.TryEnqueue(() => SyncIconFromSourceItem(sourceItem));
		}

		private void SyncIconFromSourceItem(INavigationControlItem sourceItem)
		{
			if (!_isActive)
				return;
			var icon = GetIcon(sourceItem);
			var tagIconSource = GetTagIconSource(sourceItem);
			foreach (var section in RootFolders)
			{
				if (section.SourceItem == sourceItem)
				{
					section.Icon = icon;
					section.TagIconSource = tagIconSource;
					return;
				}
				foreach (var child in section.Children)
				{
					if (child.SourceItem == sourceItem)
					{
						child.Icon = icon;
						child.TagIconSource = tagIconSource;
						return;
					}
				}
			}
		}

		private static ImageSource? GetIcon(INavigationControlItem item)
		{
			if (item is LocationItem loc)
				return loc.Icon;
			if (item is DriveItem drv)
				return drv.Icon;
			if (item is WslDistroItem wsl && wsl.Icon is not null)
				return new BitmapImage(wsl.Icon);
			return null;
		}

		private static IconSource? GetTagIconSource(INavigationControlItem item)
		{
			if (item is not FileTagItem tagItem)
				return null;
			return new PathIconSource()
			{
				Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), (string)Application.Current.Resources["App.Theme.PathIcon.FilledTag"]),
				Foreground = new SolidColorBrush(tagItem.FileTag.Color.ToColor()),
			};
		}

		private async Task ProbeHasChildrenAsync(FolderNode node)
		{
			var path = node.Path;
			var hasChildren = await Task.Run(() => FolderHelpers.HasSubfolders(path));
			if (!_isActive)
				return;
			// Only retract the chevron — never re-assert it after children have been loaded.
			if (!hasChildren && node.Children.Count == 0)
				node.HasUnrealizedChildren = false;
		}

		public void HandleItemInvoked(FolderNode fn)
		{
			if (fn.IsSection)
			{
				fn.IsExpanded = !fn.IsExpanded;
				return;
			}
			if (fn.SourceItem is not null)
			{
				SidebarViewModel.HandleItemInvokedAsync(fn.SourceItem, PointerUpdateKind.Other);
				return;
			}
			if (ContentPageContext.ShellPage is not { } page)
				return;
			page.NavigateToPath(fn.Path);
		}

		public void HandleItemContextInvoked(FrameworkElement element, ItemContextInvokedArgs args)
		{
			SidebarViewModel.HandleItemContextInvokedAsync(element, args);
		}

		public void NavigateToPath(string path)
		{
			ContentPageContext.ShellPage?.NavigateToPath(path);
		}
	}
}
