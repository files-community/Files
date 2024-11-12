// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Services
{
	internal sealed class QuickAccessService : IQuickAccessService
	{
		// Fields

		private SystemIO.FileSystemWatcher? _watcher;

		// Properties

		private readonly List<INavigationControlItem> _PinnedFolders = [];
		/// <inheritdoc/>
		public IReadOnlyList<INavigationControlItem> PinnedFolders
		{
			get
			{
				lock (_PinnedFolders)
					return _PinnedFolders.ToList().AsReadOnly();
			}
		}

		/// <inheritdoc/>
		public event EventHandler<NotifyCollectionChangedEventArgs>? PinnedFoldersChanged;

		public QuickAccessService()
		{
		}

		public async Task InitializeAsync()
		{
			_watcher = new()
			{
				Path = SystemIO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Recent), "AutomaticDestinations"),
				Filter = "f01b4d95cf55d32a.automaticDestinations-ms",
				NotifyFilter = SystemIO.NotifyFilters.DirectoryName | SystemIO.NotifyFilters.FileName | SystemIO.NotifyFilters.LastWrite,
			};

			_watcher.Changed += Watcher_Changed;
			_watcher.Deleted += Watcher_Changed;
			_watcher.EnableRaisingEvents = true;

			// TODO: Add Recycle Bin to Quick Access
		}

		public async Task<bool> UpdatePinnedFoldersAsync()
		{
			return await Task.Run(async () =>
			{
				try
				{
					List<INavigationControlItem> items = [];
					foreach (var path in GetPinnedFolders())
						items.Add(await CreateItemOf(path));

					if (items.Count is 0)
						return false;

					var snapshot = PinnedFolders;

					lock (_PinnedFolders)
					{
						_PinnedFolders.Clear();
						_PinnedFolders.AddRange(items);
					}

					var eventArgs = GetChangedActionEventArgs(snapshot, items);
					PinnedFoldersChanged?.Invoke(this, eventArgs);

					return true;
				}
				catch
				{
					return false;
				}
			});

			unsafe List<string> GetPinnedFolders()
			{
				HRESULT hr = default;

				// Get IShellItem of the shell folder
				var shellItemIid = typeof(IShellItem).GUID;
				using ComPtr<IShellItem> pFolderShellItem = default;
				fixed (char* pszFolderShellPath = "Shell:::{3936E9E4-D92C-4EEE-A85A-BC16D5EA0819}")
					hr = PInvoke.SHCreateItemFromParsingName(pszFolderShellPath, null, &shellItemIid, (void**)pFolderShellItem.GetAddressOf());

				// Get IEnumShellItems of the quick access shell folder
				var enumItemsBHID = PInvoke.BHID_EnumItems;
				Guid enumShellItemIid = typeof(IEnumShellItems).GUID;
				using ComPtr<IEnumShellItems> pEnumShellItems = default;
				hr = pFolderShellItem.Get()->BindToHandler(null, &enumItemsBHID, &enumShellItemIid, (void**)pEnumShellItems.GetAddressOf());

				// Enumerate pinned folders
				int index = 0;
				List<string> paths = [];
				using ComPtr<IShellItem> pShellItem = default;
				while (pEnumShellItems.Get()->Next(1, pShellItem.GetAddressOf()) == HRESULT.S_OK)
				{
					// Get whether the item is pined or not
					using ComPtr<IShellItem2> pShellItem2 = pShellItem.As<IShellItem2>();
					hr = PInvoke.PSGetPropertyKeyFromName("System.Home.IsPinned", out var propertyKey);
					hr = pShellItem2.Get()->GetString(propertyKey, out var szPropertyValue);
					if (bool.TryParse(szPropertyValue.ToString(), out var isPinned) && !isPinned)
						continue;

					// Get the full path
					pShellItem.Get()->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var szDisplayName);
					var path = szDisplayName.ToString();
					PInvoke.CoTaskMemFree(szDisplayName.Value);

					paths.Add(path);

					index++;
				}

				return paths;
			}

			async Task<LocationItem> CreateItemOf(string path)
			{
				var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(path));
				var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
				LocationItem locationItem;

				if (string.Equals(path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
				{
					locationItem = LocationItem.Create<RecycleBinLocationItem>();
				}
				else
				{
					locationItem = LocationItem.Create<LocationItem>();

					if (path.Equals(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
						locationItem.Text = "ThisPC".GetLocalizedResource();
					else if (path.Equals(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
						locationItem.Text = "Network".GetLocalizedResource();
				}

				locationItem.Path = path;
				locationItem.Section = SectionType.Pinned;
				locationItem.MenuOptions = new()
				{
					IsLocationItem = true,
					ShowProperties = true,
					ShowUnpinItem = true,
					ShowShellItems = true,
					ShowEmptyRecycleBin = string.Equals(path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase)
				};
				locationItem.IsDefaultLocation = false;
				locationItem.Text = res?.Result?.DisplayName ?? SystemIO.Path.GetFileName(path.TrimEnd('\\'));

				if (res)
				{
					locationItem.IsInvalid = false;
					if (res.Result is not null)
					{
						var result = await FileThumbnailHelper.GetIconAsync(
							res.Result.Path,
							Constants.ShellIconSizes.Small,
							true,
							IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

						locationItem.IconData = result;

						var bitmapImage = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => locationItem.IconData.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
						if (bitmapImage is not null)
							locationItem.Icon = bitmapImage;
					}
				}
				else
				{
					locationItem.Icon = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => UIHelpers.GetSidebarIconResource(Constants.ImageRes.Folder));
					locationItem.IsInvalid = true;
					Debug.WriteLine($"Pinned item was invalid {res?.ErrorCode}, item: {path}");
				}

				return locationItem;
			}

			NotifyCollectionChangedEventArgs GetChangedActionEventArgs(IReadOnlyList<INavigationControlItem> oldItems, IList<INavigationControlItem> newItems)
			{
				if (newItems.Count - oldItems.Count is 1)
				{
					var differences = newItems.Except(oldItems);
					if (differences.Take(2).Count() is 1)
						return new(NotifyCollectionChangedAction.Add, newItems.First());
				}
				else if (oldItems.Count - newItems.Count is 1)
				{
					var differences = oldItems.Except(newItems);
					if (differences.Take(2).Count() is 1)
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

		public async Task<bool> PinFolderAsync(string[] paths)
		{
			return await Task.Run(() =>
			{
				foreach (var path in paths)
				{
					if (!PinFolder(path))
						return false;
				}

				return true;
			});

			unsafe bool PinFolder(string path)
			{
				HRESULT hr = default;

				// Get IShellItem of the shell folder
				var shellItemIid = typeof(IShellItem).GUID;
				using ComPtr<IShellItem> pShellItem = default;
				fixed (char* pszFolderShellPath = path)
					hr = PInvoke.SHCreateItemFromParsingName(pszFolderShellPath, null, &shellItemIid, (void**)pShellItem.GetAddressOf());

				var bhid = PInvoke.BHID_SFUIObject;
				var contextMenuIid = typeof(IContextMenu).GUID;
				using ComPtr<IContextMenu> pContextMenu = default;
				hr = pShellItem.Get()->BindToHandler(null, &bhid, &contextMenuIid, (void**)pContextMenu.GetAddressOf());
				HMENU hMenu = PInvoke.CreatePopupMenu();
				hr = pContextMenu.Get()->QueryContextMenu(hMenu, 0, 1, 0x7FFF, PInvoke.CMF_OPTIMIZEFORINVOKE);

				CMINVOKECOMMANDINFO cmi = default;
				cmi.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
				cmi.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;

				fixed (byte* pVerb = Encoding.ASCII.GetBytes("pintohome"))
				{
					cmi.lpVerb = new(pVerb);
					hr = pContextMenu.Get()->InvokeCommand(cmi);
					if (hr != HRESULT.S_OK)
						return false;
				}

				return true;
			}
		}

		public async Task<bool> UnpinFolderAsync(string[] paths)
		{
			return await Task.Run(() =>
			{
				foreach (var path in paths)
				{
					if (!UnpinFolder(path))
						return false;
				}

				return true;
			});

			unsafe bool UnpinFolder(string path)
			{
				HRESULT hr = default;

				// Get IShellItem of the shell folder
				var shellItemIid = typeof(IShellItem).GUID;
				using ComPtr<IShellItem> pShellItem = default;
				fixed (char* pszFolderShellPath = path)
					hr = PInvoke.SHCreateItemFromParsingName(pszFolderShellPath, null, &shellItemIid, (void**)pShellItem.GetAddressOf());

				var bhid = PInvoke.BHID_SFUIObject;
				var contextMenuIid = typeof(IContextMenu).GUID;
				using ComPtr<IContextMenu> pContextMenu = default;
				hr = pShellItem.Get()->BindToHandler(null, &bhid, &contextMenuIid, (void**)pContextMenu.GetAddressOf());
				HMENU hMenu = PInvoke.CreatePopupMenu();
				hr = pContextMenu.Get()->QueryContextMenu(hMenu, 0, 1, 0x7FFF, PInvoke.CMF_OPTIMIZEFORINVOKE);

				CMINVOKECOMMANDINFO cmi = default;
				cmi.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
				cmi.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;

				fixed (byte* pVerb = Encoding.ASCII.GetBytes("unpinfromhome"))
				{
					cmi.lpVerb = new(pVerb);
					hr = pContextMenu.Get()->InvokeCommand(cmi);
					if (hr != HRESULT.S_OK)
						return false;
				}

				return true;
			}
		}

		private void Watcher_Changed(object sender, SystemIO.FileSystemEventArgs e)
		{
			_ = UpdatePinnedFoldersAsync();
		}
	}
}
