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

		private readonly SystemIO.FileSystemWatcher? _watcher;

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

		public WindowsRecentItemsService()
		{
			var automaticDestinationsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Recent), "AutomaticDestinations");
			
			// Only create the file system watcher if the AutomaticDestinations directory exists
			if (Directory.Exists(automaticDestinationsPath))
			{
				_watcher = new()
				{
					Path = automaticDestinationsPath,
					Filter = "5f7b5f1e01b83767.automaticDestinations-ms",
					NotifyFilter = SystemIO.NotifyFilters.DirectoryName | SystemIO.NotifyFilters.FileName | SystemIO.NotifyFilters.LastWrite,
				};

				_watcher.Changed += Watcher_Changed;
				_watcher.Deleted += Watcher_Changed;
				_watcher.EnableRaisingEvents = true;
			}
			// If the directory doesn't exist, _watcher remains null and the service will function without file system monitoring
		}

		// Methods

		/// <inheritdoc/>
		public async Task<bool> UpdateRecentFilesAsync()
		{
			return await Task.Run(() =>
			{
				return UpdateRecentItems(false);
			});
		}

		/// <inheritdoc/>
		public async Task<bool> UpdateRecentFoldersAsync()
		{
			return await Task.Run(() =>
			{
				return UpdateRecentItems(true);
			});
		}

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
				HRESULT hr = item.ShellItem.BindToHandler(null, PInvoke.BHID_SFUIObject, out IContextMenu? pContextMenu);
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

		private unsafe bool UpdateRecentItems(bool isFolder)
		{
			try
			{
				HRESULT hr = default;

				string szFolderShellPath =
					isFolder
						? "Shell:::{22877A6D-37A1-461A-91B0-DBDA5AAEBC99}"  // Recent Places folder (recent folders)
						: "Shell:::{679F85CB-0220-4080-B29B-5540CC05AAB6}"; // Quick Access folder (recent files)

				// Get IShellItem of the shell folder
				hr = PInvoke.SHCreateItemFromParsingName(szFolderShellPath, null, out IShellItem pFolderShellItem);
				if (hr.Failed)
					return false;

				// Get IEnumShellItems of the quick access shell folder
				hr = pFolderShellItem.BindToHandler(null, PInvoke.BHID_EnumItems, out IEnumShellItems? pEnumShellItems);
				if (hr.Failed || pEnumShellItems is null)
					return false;

				// Enumerate recent items and populate the list
				int index = 0;
				List<RecentItem> recentItems = [];
				IShellItem[] pShellItems = new IShellItem[1];
				while (pEnumShellItems.Next(1, pShellItems, null) == HRESULT.S_OK)
				{
					IShellItem shellItem = pShellItems[0];

					// Get top 20 items
					if (index is 20)
						break;

					// Exclude folders, but keep archives (ZIP/7z/RAR) which the shell reports as both folder and stream.
					if (shellItem.GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER | SFGAO_FLAGS.SFGAO_STREAM, out var attribute).Succeeded &&
						(attribute & SFGAO_FLAGS.SFGAO_FOLDER) == SFGAO_FLAGS.SFGAO_FOLDER &&
						(attribute & SFGAO_FLAGS.SFGAO_STREAM) == 0)
						continue;

					// Get the target path
					shellItem.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEEDITING, out var szDisplayName);
					var targetPath = szDisplayName.ToString();
					PInvoke.CoTaskMemFree(szDisplayName.Value);

					// Get the display name
					shellItem.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY, out szDisplayName);
					var fileName = szDisplayName.ToString();
					PInvoke.CoTaskMemFree(szDisplayName.Value);

					// Strip the file extension except when the file name only contains extension (e.g. ".gitignore")
					if (!FoldersSettingsService.ShowFileExtensions &&
						SystemIO.Path.GetFileNameWithoutExtension(fileName) is string fileNameWithoutExtension)
						fileName = string.IsNullOrEmpty(fileNameWithoutExtension) ? SystemIO.Path.GetFileName(fileName) : fileNameWithoutExtension;

					// Get the date last modified
					DateTime lastModified = DateTime.MinValue;

					if (shellItem is IShellItem2 shellItem2 &&
						PInvoke.PSGetPropertyKeyFromName("System.DateModified", out var propertyKey).Succeeded)
					{
						hr = shellItem2.GetString(propertyKey, out var szPropertyValue);
						if (hr.Succeeded)
						{
							if (!DateTime.TryParse(szPropertyValue.ToString(), out lastModified))
								lastModified = DateTime.MinValue;

							PInvoke.CoTaskMemFree(szPropertyValue);
						}
					}

					recentItems.Add(new()
					{
						Path = targetPath,
						Name = fileName,
						ShellItem = shellItem,
						LastModified = lastModified,
					});

					index++;
				}

				if (recentItems.Count is 0)
					return false;

				var snapshot = isFolder ? RecentFolders : RecentFiles;

				if (isFolder)
				{
					lock (_RecentFolders)
					{
						_RecentFolders.Clear();
						_RecentFolders.AddRange(recentItems);
					}
				}
				else
				{
					lock (_RecentFiles)
					{
						_RecentFiles.Clear();
						_RecentFiles.AddRange(recentItems);
					}
				}

				var eventArgs = GetChangedActionEventArgs(snapshot, recentItems);

				if (isFolder)
					RecentFoldersChanged?.Invoke(this, eventArgs);
				else
					RecentFilesChanged?.Invoke(this, eventArgs);

				return true;
			}
			catch
			{
				return false;
			}
		}

		private void Watcher_Changed(object sender, SystemIO.FileSystemEventArgs e)
		{
			_ = UpdateRecentFilesAsync();
			_ = UpdateRecentFoldersAsync();
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
