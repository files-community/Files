using Files.App.Helpers;
using Files.App.Shell;
using Files.Core.Extensions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Filesystem
{
	public class RecentItems : IDisposable
	{
		private const string QuickAccessGuid = "::{679f85cb-0220-4080-b29b-5540cc05aab6}";

		public EventHandler<NotifyCollectionChangedEventArgs>? RecentFilesChanged;
		public EventHandler<NotifyCollectionChangedEventArgs>? RecentFoldersChanged;

		// recent files
		private readonly List<RecentItem> recentFiles = new();
		public IReadOnlyList<RecentItem> RecentFiles    // already sorted
		{
			get
			{
				lock (recentFiles)
				{
					return recentFiles.ToList().AsReadOnly();
				}
			}
		}

		// recent folders
		private readonly List<RecentItem> recentFolders = new();
		public IReadOnlyList<RecentItem> RecentFolders  // already sorted
		{
			get
			{
				lock (recentFolders)
				{
					return recentFolders.ToList().AsReadOnly();
				}
			}
		}

		public RecentItems()
		{
			RecentItemsManager.Default.RecentItemsChanged += OnRecentItemsChanged;
		}

		private async void OnRecentItemsChanged(object? sender, EventArgs e)
		{
			await UpdateRecentFilesAsync();
		}

		/// <summary>
		/// Refetch recent files to `recentFiles`.
		/// </summary>
		public async Task UpdateRecentFilesAsync()
		{
			// enumerate with fulltrust process
			List<RecentItem> enumeratedFiles = await ListRecentFilesAsync();
			if (enumeratedFiles is not null)
			{
				var recentFilesSnapshot = RecentFiles;

				lock (recentFiles)
				{
					recentFiles.Clear();
					recentFiles.AddRange(enumeratedFiles);
					// do not sort here, enumeration order *is* the correct order since we get it from Quick Access
				}

				var changedActionEventArgs = GetChangedActionEventArgs(recentFilesSnapshot, enumeratedFiles);
				RecentFilesChanged?.Invoke(this, changedActionEventArgs);
			}
		}

		/// <summary>
		/// Refetch recent folders to `recentFolders`.
		/// </summary>
		public async Task UpdateRecentFoldersAsync()
		{
			var enumeratedFolders = await Task.Run(ListRecentFoldersAsync);	// run off the UI thread
			if (enumeratedFolders is not null)
			{
				lock (recentFolders)
				{
					recentFolders.Clear();
					recentFolders.AddRange(enumeratedFolders);

					// shortcut modifications in `Windows\Recent` consist of a delete + add operation;
					// thus, last modify date is reset and we can sort off it
					recentFolders.Sort((x, y) => y.LastModified.CompareTo(x.LastModified));
				}

				RecentFoldersChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
		}

		/// <summary>
		/// Enumerate recently accessed files via `Quick Access`.
		/// </summary>
		public async Task<List<RecentItem>> ListRecentFilesAsync()
		{
			return (await Win32Shell.GetShellFolderAsync(QuickAccessGuid, "Enumerate", 0, int.MaxValue)).Enumerate
				.Where(link => !link.IsFolder)
				.Select(link => new RecentItem(link)).ToList();
		}

		/// <summary>
		/// Enumerate recently accessed folders via `Windows\Recent`.
		/// </summary>
		public async Task<List<RecentItem>> ListRecentFoldersAsync()
		{
			var recentItems = new List<RecentItem>();
			var excludeMask = FileAttributes.Hidden;
			var linkFilePaths = Directory.EnumerateFiles(CommonPaths.RecentItemsPath).Where(f => (new FileInfo(f).Attributes & excludeMask) == 0);

			Task<RecentItem?> GetRecentItemFromLink(string linkPath)
			{
				return Task.Run(() =>
				{
					try
					{
						using var link = new ShellLink(linkPath, LinkResolution.NoUIWithMsgPump, default, TimeSpan.FromMilliseconds(100));

						if (!string.IsNullOrEmpty(link.TargetPath) && link.Target.IsFolder)
						{
							var shellLinkItem = ShellFolderExtensions.GetShellLinkItem(link);
							return new RecentItem(shellLinkItem);
						}
					}
					catch (FileNotFoundException)
					{
						// occurs when shortcut or shortcut target is deleted and accessed (link.Target)
						// consequently, we shouldn't include the item as a recent item
					}
					catch (Exception ex)
					{
						App.Logger.Warn(ex, ex.Message);
					}

					return null;
				});
			}

			var recentFolderTasks = linkFilePaths.Select(GetRecentItemFromLink);
			var result = await Task.WhenAll(recentFolderTasks);

			return result.OfType<RecentItem>().ToList();
		}

		/// <summary>
		/// Adds a shortcut to `Windows\Recent`. The path can be to a file or folder.
		/// It will update to `recentFiles` or `recentFolders` respectively.
		/// </summary>
		/// <param name="path">Path to a file or folder</param>
		/// <returns>Whether the action was successfully handled or not</returns>
		public bool AddToRecentItems(string path)
		{
			try
			{
				Shell32.SHAddToRecentDocs(Shell32.SHARD.SHARD_PATHW, path);
				return true;
			}
			catch (Exception ex)
			{
				App.Logger.Warn(ex, ex.Message);
				return false;
			}
		}

		/// <summary>
		/// Clears both `recentFiles` and `recentFolders`.
		/// This will also clear the Recent Files (and its jumplist) in File Explorer.
		/// </summary>
		/// <returns>Whether the action was successfully handled or not</returns>
		public bool ClearRecentItems()
		{
			try
			{
				Shell32.SHAddToRecentDocs(Shell32.SHARD.SHARD_PIDL, (string)null);
				return true;
			}
			catch (Exception ex)
			{
				App.Logger.Warn(ex, ex.Message);
				return false;
			}
		}

		/// <summary>
		/// Unpin (or remove) a file from `recentFiles`.
		/// This will also unpin the item from the Recent Files in File Explorer.
		/// </summary>
		/// <returns>Whether the action was successfully handled or not</returns>
		public Task<bool> UnpinFromRecentFiles(RecentItem item)
		{
			return SafetyExtensions.IgnoreExceptions(() => Task.Run(async () =>
			{
				using var pidl = new Shell32.PIDL(item.PIDL);
				using var shellItem = ShellItem.Open(pidl);
				using var cMenu = await ContextMenu.GetContextMenuForFiles(new[] { shellItem }, Shell32.CMF.CMF_NORMAL);
				if (cMenu is not null)
					return await cMenu.InvokeVerb("remove");
				return false;
			}));
		}

		private NotifyCollectionChangedEventArgs GetChangedActionEventArgs(IReadOnlyList<RecentItem> oldItems, IList<RecentItem> newItems)
		{
			// a single item was added
			if (newItems.Count == oldItems.Count + 1)
			{
				var differences = newItems.Except(oldItems);
				if (differences.Take(2).Count() == 1)
				{
					return new(NotifyCollectionChangedAction.Add, newItems.First());
				}
			}
			// a single item was removed
			else if (newItems.Count == oldItems.Count - 1)
			{
				var differences = oldItems.Except(newItems);
				if (differences.Take(2).Count() == 1)
				{
					for (int i = 0; i < oldItems.Count; i++)
					{
						if (i >= newItems.Count || !newItems[i].Equals(oldItems[i]))
						{
							return new(NotifyCollectionChangedAction.Remove, oldItems[i], index: i);
						}
					}
				}
			}
			// a single item was moved
			else if (newItems.Count == oldItems.Count)
			{
				var differences = oldItems.Except(newItems);
				// desync due to skipped/batched calls, reset the list
				if (differences.Any())
				{
					return new(NotifyCollectionChangedAction.Reset);
				}

				// first diff from reversed is the designated item
				for (int i = oldItems.Count - 1; i >= 0; i--)
				{
					if (!oldItems[i].Equals(newItems[i]))
					{
						return new(NotifyCollectionChangedAction.Move, oldItems[i], index: 0, oldIndex: i);
					}
				}
			}

			return new(NotifyCollectionChangedAction.Reset);
		}

		public bool CheckIsRecentFilesEnabled()
		{
			using var subkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
			using var advSubkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
			using var userPolicySubkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer");
			using var sysPolicySubkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer");

			if (subkey is not null)
			{
				// quick access: show recent files option
				bool showRecentValue = Convert.ToBoolean(subkey.GetValue("ShowRecent", true)); // 1 by default
				if (!showRecentValue)
				{
					return false;
				}
			}

			if (advSubkey is not null)
			{
				// settings: personalization > start > show recently opened items
				bool startTrackDocsValue = Convert.ToBoolean(advSubkey.GetValue("Start_TrackDocs", true)); // 1 by default
				if (!startTrackDocsValue)
				{
					return false;
				}
			}

			// for users in group policies
			var policySubkey = userPolicySubkey ?? sysPolicySubkey;
			if (policySubkey is not null)
			{
				bool noRecentDocsHistoryValue = Convert.ToBoolean(policySubkey.GetValue("NoRecentDocsHistory", false)); // 0 by default
				if (noRecentDocsHistoryValue)
				{
					return false;
				}
			}

			return true;
		}

		public void Dispose()
		{
			RecentItemsManager.Default.RecentItemsChanged -= OnRecentItemsChanged;
		}
	}
}