using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Shell;
using Files.Backend.Services;
using Files.Sdk.Storage.LocatableStorage;
using Files.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.ServicesImplementation
{
	public class RecentItemsService : IRecentItemsService
	{
		private const string QuickAccessGuid = "::{679f85cb-0220-4080-b29b-5540cc05aab6}";

		public bool AddToRecentItems(string path)
		{
			try
			{
				Shell32.SHAddToRecentDocs(Shell32.SHARD.SHARD_PATHW, path);
				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return false;
			}
		}

		public bool ClearRecentItems()
		{
			try
			{
				Shell32.SHAddToRecentDocs(Shell32.SHARD.SHARD_PIDL, (string)null);
				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return false;
			}
		}

		public bool IsSupported()
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

		public async Task<IList<ILocatableStorable>> ListRecentFilesAsync()
		{
			return (await Win32Shell.GetShellFolderAsync(QuickAccessGuid, "Enumerate", 0, int.MaxValue)).Enumerate
							.Where(link => !link.IsFolder)
							.Select(link => new RecentItem(link)).Cast<ILocatableStorable>().ToList();
		}

		public async Task<IList<ILocatableStorable>> ListRecentFoldersAsync()
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
						App.Logger.LogWarning(ex, ex.Message);
					}

					return null;
				});
			}

			var recentFolderTasks = linkFilePaths.Select(GetRecentItemFromLink);
			var result = await Task.WhenAll(recentFolderTasks);

			return result.OfType<RecentItem>().Cast<ILocatableStorable>().ToList();
		}

		public Task<bool> UnpinFromRecentFilesAsync(ILocatableStorable item)
		{
			return SafetyExtensions.IgnoreExceptions(() => Task.Run(async () =>
			{
				using var pidl = new Shell32.PIDL(((RecentItem)item).PIDL);
				using var shellItem = ShellItem.Open(pidl);
				using var cMenu = await ContextMenu.GetContextMenuForFiles(new[] { shellItem }, Shell32.CMF.CMF_NORMAL);
				if (cMenu is not null)
					return await cMenu.InvokeVerb("remove");
				return false;
			}));
		}

		private NotifyCollectionChangedEventArgs GetChangedActionEventArgs(IReadOnlyList<ILocatableStorable> oldItems, IList<ILocatableStorable> newItems)
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

		public async Task<NotifyCollectionChangedEventArgs> UpdateRecentFilesAsync(List<ILocatableStorable> itemsCollection)
		{
			List<RecentItem> enumeratedFiles = (await ListRecentFilesAsync()).Cast<RecentItem>().ToList();
			if (enumeratedFiles is not null)
			{
				var recentFilesSnapshot = itemsCollection.ToList();

				lock (itemsCollection)
				{
					itemsCollection.Clear();
					itemsCollection.AddRange(enumeratedFiles);
					// do not sort here, enumeration order *is* the correct order since we get it from Quick Access
				}

				return GetChangedActionEventArgs(recentFilesSnapshot, itemsCollection);
			}

			return null;
		}

		public async Task<NotifyCollectionChangedEventArgs> UpdateRecentFoldersAsync(List<ILocatableStorable> itemsCollection)
		{
			var enumeratedFolders = await Task.Run(ListRecentFoldersAsync); // run off the UI thread
			if (enumeratedFolders is not null)
			{
				var recentFoldersSnapshot = itemsCollection.ToList();

				lock (itemsCollection)
				{
					itemsCollection.Clear();
					itemsCollection.AddRange(enumeratedFolders);

					// shortcut modifications in `Windows\Recent` consist of a delete + add operation;
					// thus, last modify date is reset and we can sort off it
					itemsCollection.Cast<RecentItem>().ToList().Sort((x, y) => y.LastModified.CompareTo(x.LastModified));
				}

				return GetChangedActionEventArgs(recentFoldersSnapshot, itemsCollection);
			}

			return null;
		}
	}
}
