// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsRecentItemsService"/>
	public class WindowsRecentItemsService : IWindowsRecentItemsService
	{
		// Dependency injections

		private readonly IFoldersSettingsService FoldersSettingsService = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		// Fields


		// Properties

		private readonly List<RecentItem> _RecentFiles = [];
		/// <inheritdoc/>
		public IReadOnlyList<RecentItem> RecentFiles
		{
			get
			{
				lock (_RecentFiles)
					return _RecentFiles.ToList().AsReadOnly();
			}
		}

		private readonly List<RecentItem> _RecentFolders = [];
		/// <inheritdoc/>
		public IReadOnlyList<RecentItem> RecentFolders
		{
			get
			{
				lock (_RecentFolders)
					return _RecentFolders.ToList().AsReadOnly();
			}
		}

		// Events 

		/// <inheritdoc/>
		public event EventHandler<NotifyCollectionChangedEventArgs>? RecentFilesChanged;

		/// <inheritdoc/>
		public event EventHandler<NotifyCollectionChangedEventArgs>? RecentFoldersChanged;

		// Constructor

		public WindowsRecentItemsService() => App.ServerChanged += OnServerChanged;

		private void OnServerChanged(object? sender, string name)
		{
			if (name != ServerChangeNames.RecentItems)
				return;
			_ = SafetyExtensions.IgnoreExceptions(UpdateRecentFilesAsync, App.Logger);
			_ = SafetyExtensions.IgnoreExceptions(UpdateRecentFoldersAsync, App.Logger);
		}

		// Methods

		/// <inheritdoc/>
		public Task<bool> UpdateRecentFilesAsync() => UpdateRecentItemsAsync(false);

		/// <inheritdoc/>
		public Task<bool> UpdateRecentFoldersAsync() => UpdateRecentItemsAsync(true);

		/// <inheritdoc/>
		public unsafe bool Add(string path)
		{
			try
			{
				fixed (char* cPath = path)
					PInvoke.SHAddToRecentDocs((uint)SHARD.SHARD_PATHW, cPath);

				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return false;
			}
		}

		/// <inheritdoc/>
		public unsafe bool Remove(RecentItem item)
		{
			try
			{
				using var shellItem = ShellItem.Open(new ShellPidl(item.Pidl));
				HRESULT hr = shellItem.IShellItem.BindToHandler(null, PInvoke.BHID_SFUIObject, out IContextMenu? pContextMenu);
				if (hr.Failed || pContextMenu is null)
					return false;

				HMENU hMenu = PInvoke.CreatePopupMenu();
				hr = pContextMenu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, PInvoke.CMF_OPTIMIZEFORINVOKE);

				// Initialize invocation info
				CMINVOKECOMMANDINFO cmi = default;
				cmi.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
				cmi.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;

				// Unpin the item
				fixed (byte* pVerb1 = Encoding.ASCII.GetBytes("remove"),
					pVerb2 = Encoding.ASCII.GetBytes("unpinfromhome"),
					pVerb3 = Encoding.ASCII.GetBytes("removefromhome"))
				{
					// Try unpin files
					cmi.lpVerb = new(pVerb1);
					hr = pContextMenu.InvokeCommand(cmi);
					if (hr == HRESULT.S_OK)
						return true;

					// Try unpin folders
					cmi.lpVerb = new(pVerb2);
					hr = pContextMenu.InvokeCommand(cmi);
					if (hr == HRESULT.S_OK)
						return true;

					// NOTE:
					//  There seems to be an issue with unpinfromhome where some shell folders
					//  won't be removed via unpinfromhome verb.
					// Try unpin folders again
					cmi.lpVerb = new(pVerb3);
					hr = pContextMenu.InvokeCommand(cmi);
					if (hr == HRESULT.S_OK)
						return true;
				}

				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return false;
			}
		}

		/// <inheritdoc/>
		public unsafe bool Clear()
		{
			try
			{
				PInvoke.SHAddToRecentDocs((uint)SHARD.SHARD_PIDL, null);

				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return false;
			}
		}

		private async Task<bool> UpdateRecentItemsAsync(bool folders)
		{
			var entries = await Server.AppService.GetRecentItemsAsync(folders);
			var items = entries.Select(entry => new RecentItem
			{
				Path = entry.Path,
				Name = FoldersSettingsService.ShowFileExtensions || string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(entry.Name))
					? entry.Name : Path.GetFileNameWithoutExtension(entry.Name),
				LastModified = entry.Modified == DateTimeOffset.MinValue ? DateTime.MinValue : entry.Modified.LocalDateTime,
				Pidl = entry.Pidl,
			}).ToList();
			var previous = folders ? RecentFolders : RecentFiles;
			var target = folders ? _RecentFolders : _RecentFiles;
			lock (target)
			{
				target.Clear();
				target.AddRange(items);
			}
			var change = GetChangedActionEventArgs(previous, items);
			if (folders)
				RecentFoldersChanged?.Invoke(this, change);
			else
				RecentFilesChanged?.Invoke(this, change);
			return true;
		}

		private NotifyCollectionChangedEventArgs GetChangedActionEventArgs(IReadOnlyList<RecentItem> oldItems, IList<RecentItem> newItems)
		{
			if (newItems.Count - oldItems.Count is 1)
			{
				var differences = newItems.Except(oldItems);
				if (differences.Take(2).Count() == 1)
					return new(NotifyCollectionChangedAction.Add, newItems.First());
			}
			else if (oldItems.Count - newItems.Count is 1)
			{
				var differences = oldItems.Except(newItems);
				if (differences.Take(2).Count() == 1)
				{
					for (int i = 0; i < oldItems.Count; i++)
					{
						if (i >= newItems.Count || !newItems[i].Equals(oldItems[i]))
							return new(NotifyCollectionChangedAction.Remove, oldItems[i], index: i);
					}
				}
			}
			else if (newItems.Count == oldItems.Count)
			{
				var differences = oldItems.Except(newItems);
				if (differences.Any())
					return new(NotifyCollectionChangedAction.Reset);

				// First diff from reversed is the designated item
				for (int i = oldItems.Count - 1; i >= 0; i--)
				{
					if (!oldItems[i].Equals(newItems[i]))
						return new(NotifyCollectionChangedAction.Move, oldItems[i], index: 0, oldIndex: i);
				}
			}

			return new(NotifyCollectionChangedAction.Reset);
		}
	}
}
