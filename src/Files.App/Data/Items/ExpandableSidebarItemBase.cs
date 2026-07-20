// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Shared expansion plumbing for tree-view-style sidebar items (LocationItem, DriveItem).
	/// Stores IsExpandableFolder / HasUnrealizedChildren / IsExpanded and runs the async lazy
	/// subfolder load on first expansion. Subclasses provide the path + child collection via
	/// abstract members and keep their own Children getter (Home / non-expandable cases vary).
	/// </summary>
	public abstract partial class ExpandableSidebarItemBase : ObservableObject
	{
		protected abstract string ExpansionPath { get; }
		protected abstract BulkConcurrentObservableCollection<INavigationControlItem> EnsureChildItems();

		private bool isExpandableFolder;
		public bool IsExpandableFolder
		{
			get => isExpandableFolder;
			set
			{
				if (SetProperty(ref isExpandableFolder, value))
				{
					OnPropertyChanged(nameof(ISidebarItemModel.Children));
					OnPropertyChanged(nameof(IsLeafWithChildren));
					// Tab-state restoration may have flipped IsExpanded before expandability was known — drive the lazy load now.
					if (value && isExpanded && !childrenLoaded && !childrenLoading)
						_ = LoadSubfoldersAsync();
				}
			}
		}

		private bool hasUnrealizedChildren;
		public bool HasUnrealizedChildren
		{
			get => hasUnrealizedChildren;
			set => SetProperty(ref hasUnrealizedChildren, value);
		}

		public bool IsLeafWithChildren => IsExpandableFolder;

		private bool childrenLoaded;
		private bool childrenLoading;

		private bool isExpanded;
		public bool IsExpanded
		{
			get => isExpanded;
			set
			{
				if (!SetProperty(ref isExpanded, value))
					return;

				if (value)
				{
					if (IsExpandableFolder && !childrenLoaded && !childrenLoading)
					{
						_ = LoadSubfoldersAsync();
					}
					else if (childrenLoaded)
					{
						// Re-expanded after a collapse: watcher was stopped, so do a single catch-up resync (bypassing the debounce) to pick up any changes that happened while invisible, then resume live watching.
						StartWatchingSubfolders();
						_ = ResyncSubfoldersAsync();
					}
				}
				else
				{
					// Children aren't in the flat tree while collapsed; StopWatchingSubfolders disposes the watcher + debounce timer and cancels any in-flight resync via the generation counter.
					StopWatchingSubfolders();
				}
			}
		}

		public async Task LoadSubfoldersAsync()
		{
			if (childrenLoaded || childrenLoading)
				return;
			childrenLoading = true;
			try
			{
				await LocationItem.LoadSubfoldersIntoAsync(ExpansionPath, EnsureChildItems(), () =>
				{
					HasUnrealizedChildren = false;
					childrenLoaded = true;
					StartWatchingSubfolders();
				});
			}
			finally
			{
				childrenLoading = false;
			}
		}

		public bool IsLoaded => childrenLoaded;

		// Wipes the cached child collection and (if still expanded) re-runs LoadSubfoldersAsync so the next enumeration picks up the latest IUserSettingsService.FoldersSettingsService filter flags. The tab-expansion tracker re-expands matching descendants as they get re-added, so deep subtrees fan back out without an explicit walk.
		public async Task ReloadSubfoldersAsync()
		{
			if (!childrenLoaded)
				return;
			StopWatchingSubfoldersAndDescendants();
			EnsureChildItems().Clear();
			childrenLoaded = false;
			if (IsExpanded && IsExpandableFolder)
				await LoadSubfoldersAsync();
		}

		#region Subfolder watcher

		// Generous debounce: the sidebar tree is a navigation aid, not a real-time mirror; waiting a few seconds before resyncing coalesces bursts (extract / clone / build) into a single enumeration.
		private static readonly TimeSpan ResyncDebounce = TimeSpan.FromSeconds(3);

		private FileSystemWatcher? subfolderWatcher;
		private DispatcherQueueTimer? resyncDebounceTimer;
		// Incremented every time the watcher (re)starts or stops. ResyncSubfoldersAsync captures the value on entry and aborts at every await if it no longer matches — this cancels in-flight resyncs when the row is collapsed, when ReloadSubfoldersAsync wipes ChildItems, or when a parent pruned this item out of the tree.
		private int resyncGeneration;

		// Starts watching the folder for child folder add / remove / rename so the sidebar reflects filesystem changes without an app restart. No-op when the path doesn't exist (drives that aren't ready, ejected media, etc.) or watcher construction fails (UnauthorizedAccessException on protected roots).
		private void StartWatchingSubfolders()
		{
			if (subfolderWatcher is not null || !Directory.Exists(ExpansionPath))
				return;

			try
			{
				subfolderWatcher = new FileSystemWatcher(ExpansionPath) { NotifyFilter = NotifyFilters.DirectoryName };
				subfolderWatcher.Created += OnSubfolderWatcherEvent;
				subfolderWatcher.Deleted += OnSubfolderWatcherEvent;
				subfolderWatcher.Renamed += OnSubfolderWatcherEvent;
				subfolderWatcher.EnableRaisingEvents = true;
			}
			// FileSystemWatcher ctor throws ArgumentException for invalid paths, UnauthorizedAccessException for protected roots; either way fall back to no live updates for this subtree.
			catch (Exception ex)
			{
				App.Logger?.LogDebug(ex, "Sidebar subfolder watcher start failed for {Path}", ExpansionPath);
				subfolderWatcher?.Dispose();
				subfolderWatcher = null;
			}
		}

		private void StopWatchingSubfolders()
		{
			// Bumping the generation cancels any in-flight ResyncSubfoldersAsync after its next await — without this, a watcher tick fired just before collapse / prune would resume on the dispatcher and mutate ChildItems that nothing is observing.
			Interlocked.Increment(ref resyncGeneration);

			if (resyncDebounceTimer is not null)
			{
				try { resyncDebounceTimer.Stop(); }
				catch (Exception ex) { App.Logger?.LogDebug(ex, "Sidebar subfolder debounce timer stop failed"); }
				resyncDebounceTimer = null;
			}

			if (subfolderWatcher is null)
				return;
			// Dispose stops raising events and releases the OS notification subscription; the catch covers ObjectDisposedException / IOException if the watched folder has gone away.
			try { subfolderWatcher.Dispose(); }
			catch (Exception ex) { App.Logger?.LogDebug(ex, "Sidebar subfolder watcher dispose failed"); }
			subfolderWatcher = null;
		}

		// Called by a parent's ReloadSubfoldersAsync (and by the section-sync remove paths in SidebarViewModel) before clearing/removing items; without this, descendants would be detached from the tree but keep firing into orphaned timers/handlers.
		internal void StopWatchingSubfoldersAndDescendants()
		{
			StopWatchingSubfolders();
			if (!childrenLoaded)
				return;
			foreach (var child in EnsureChildItems().OfType<ExpandableSidebarItemBase>())
				child.StopWatchingSubfoldersAndDescendants();
		}

		// Fires on a thread-pool thread; FileSystemWatcher can fan out a single mkdir into Created + LastWrite. Marshal to UI + debounce so a burst yields one resync.
		private void OnSubfolderWatcherEvent(object sender, FileSystemEventArgs e)
		{
			var dispatcher = MainWindow.Instance?.DispatcherQueue;
			if (dispatcher is null)
				return;
			dispatcher.TryEnqueue(() =>
			{
				if (resyncDebounceTimer is null)
				{
					resyncDebounceTimer = dispatcher.CreateTimer();
					resyncDebounceTimer.Interval = ResyncDebounce;
					resyncDebounceTimer.Tick += async (t, _) => { t.Stop(); await ResyncSubfoldersAsync(); };
				}
				resyncDebounceTimer.Stop();
				resyncDebounceTimer.Start();
			});
		}

		// Reconciles ChildItems with the live filesystem state via a single merge walk over two name-sorted sequences (collection by LocationItem.Text, freshEntries by SubfolderEntry.Name, both OrdinalIgnoreCase). New rows paint with the cached generic folder icon up front; per-path custom icons are background-upgraded after — same two-phase paint as the initial load.
		private async Task ResyncSubfoldersAsync()
		{
			// Children are off-screen while collapsed; the IsExpanded=true branch fires a catch-up resync when the row reopens.
			if (!childrenLoaded || !IsExpanded)
				return;

			// Capture the current generation; if the watcher is stopped or restarted before we finish, the comparisons below short-circuit so we never mutate a collapsed / orphaned ChildItems.
			var generation = Volatile.Read(ref resyncGeneration);

			var showHidden = Ioc.Default.GetService<IUserSettingsService>()?.FoldersSettingsService?.ShowHiddenItems ?? false;
			List<SubfolderEntry> freshEntries;
			try
			{
				freshEntries = await Task.Run(() => FolderHelpers.EnumerateSubfolders(ExpansionPath, showHidden, showProtected: false, showDot: false));
			}
			// EnumerateSubfolders can throw UnauthorizedAccessException / IOException if the folder is in a bad state mid-resync; treat as "no change visible right now" rather than tearing down ChildItems.
			catch (Exception ex)
			{
				App.Logger?.LogDebug(ex, "Sidebar subfolder resync enumeration failed for {Path}", ExpansionPath);
				return;
			}

			if (IsResyncCancelled(generation))
				return;

			var iconBytes = await LocationItem.GetGenericSmallFolderIconBytesAsync();
			var dispatcher = MainWindow.Instance?.DispatcherQueue;
			if (dispatcher is null || IsResyncCancelled(generation))
				return;

			List<LocationItem>? inserted = null;

			await dispatcher.EnqueueOrInvokeAsync(async () =>
			{
				if (IsResyncCancelled(generation))
					return;

				var collection = EnsureChildItems();
				var cmp = StringComparer.OrdinalIgnoreCase;
				BitmapImage? sharedIcon = null;
				var iconAttempted = false;
				var ci = 0;
				var fi = 0;

				void Prune()
				{
					if (collection[ci] is ExpandableSidebarItemBase orphan)
						orphan.StopWatchingSubfoldersAndDescendants();
					collection.RemoveAt(ci);
				}

				async Task InsertAsync(SubfolderEntry entry)
				{
					// Decode the shared bitmap lazily: a delete-only resync skips it entirely.
					if (!iconAttempted)
					{
						iconAttempted = true;
						if (iconBytes is not null)
						{
							try { sharedIcon = await iconBytes.ToBitmapAsync(); }
							// BitmapImage.SetSourceAsync throws on corrupt bytes; let new rows render iconless rather than skip the structural sync.
							catch (Exception ex) { App.Logger?.LogDebug(ex, "Sidebar resync shared folder icon decode failed"); }
						}
					}
					var newItem = LocationItem.CreateSubfolder(entry, sharedIcon);
					collection.Insert(ci, newItem);
					(inserted ??= []).Add(newItem);
					ci++;
				}

				while (ci < collection.Count && fi < freshEntries.Count)
				{
					if (IsResyncCancelled(generation))
						return;
					var order = cmp.Compare((collection[ci] as LocationItem)?.Text ?? string.Empty, freshEntries[fi].Name);
					if (order == 0) { ci++; fi++; }
					else if (order < 0) Prune();
					else { await InsertAsync(freshEntries[fi]); fi++; }
				}
				while (ci < collection.Count && !IsResyncCancelled(generation)) Prune();
				while (fi < freshEntries.Count && !IsResyncCancelled(generation)) { await InsertAsync(freshEntries[fi]); fi++; }
			});

			if (inserted is not null && !IsResyncCancelled(generation))
				_ = LocationItem.UpgradeIconsAsync(inserted, iconBytes, dispatcher);
		}

		// Centralizes the "this resync is still relevant" check so every await/loop boundary aborts uniformly when the watcher was stopped or restarted under us.
		private bool IsResyncCancelled(int generation)
			=> generation != Volatile.Read(ref resyncGeneration) || !childrenLoaded || !IsExpanded;

		#endregion
	}
}
