// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;

namespace Files.App.Services
{
	internal sealed class QuickAccessService : IQuickAccessService
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Fields

		private readonly SemaphoreSlim _addSyncSemaphore = new(1, 1);

		private readonly SystemIO.FileSystemWatcher _quickAccessFolderWatcher;

		// Properties

		/// <inheritdoc/>
		public List<string> PinnedFolders { get; set; } = [];

		private readonly List<INavigationControlItem> _PinnedFolderItems = [];
		/// <inheritdoc/>
		public IReadOnlyList<INavigationControlItem> PinnedFolderItems
		{
			get
			{
				lock (_PinnedFolderItems)
					return _PinnedFolderItems.ToList().AsReadOnly();
			}
		}

		// Events

		/// <inheritdoc/>
		public event EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		/// <inheritdoc/>
		public event SystemIO.FileSystemEventHandler? PinnedItemsModified;

		/// <inheritdoc/>
		public event EventHandler<ModifyQuickAccessEventArgs>? UpdateQuickAccessWidget;

		public QuickAccessService()
		{
			_quickAccessFolderWatcher = new()
			{
				Path = SystemIO.Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent", "AutomaticDestinations"),
				Filter = "f01b4d95cf55d32a.automaticDestinations-ms",
				NotifyFilter = SystemIO.NotifyFilters.LastAccess | SystemIO.NotifyFilters.LastWrite | SystemIO.NotifyFilters.FileName,
				EnableRaisingEvents = true
			};

			_quickAccessFolderWatcher.Changed += PinnedItemsWatcher_Changed;
		}

		/// <inheritdoc/>
		public async Task InitializeAsync()
		{
			PinnedItemsModified += LoadAsync;

			//if (!Model.PinnedFolders.Contains(Constants.UserEnvironmentPaths.RecycleBinPath) && SystemInformation.Instance.IsFirstRun)
			//	await QuickAccessService.PinToSidebar(Constants.UserEnvironmentPaths.RecycleBinPath);

			await UpdateItemsWithExplorerAsync();
		}

		/// <inheritdoc/>
		public async Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync()
		{
			return (await Win32Helper.GetShellFolderAsync(Constants.CLID.QuickAccess, false, true, 0, int.MaxValue, "System.Home.IsPinned"))
				.Enumerate
				.Where(link => link.IsFolder);
		}

		/// <inheritdoc/>
		public async Task PinToSidebarAsync(string[] folderPaths, bool invokeQuickAccessChangedEvent = true)
		{
			foreach (string folderPath in folderPaths)
				await ContextMenu.InvokeVerb("pintohome", [folderPath]);

			await UpdateItemsWithExplorerAsync();

			if (invokeQuickAccessChangedEvent)
				UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, true));
		}

		/// <inheritdoc/>
		public async Task UnpinFromSidebarAsync(string[] folderPaths, bool invokeQuickAccessChangedEvent = true)
		{
			var shellAppType = Type.GetTypeFromProgID("Shell.Application")!;

			var shell = Activator.CreateInstance(shellAppType);

			dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, [$"shell:{Constants.CLID.QuickAccess}"]);

			if (f2 is null)
				return;

			if (folderPaths.Length == 0)
				folderPaths = (await GetPinnedFoldersAsync())
					.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
					.Select(link => link.FilePath).ToArray();

			foreach (var fi in f2.Items())
			{
				if (ShellStorageFolder.IsShellPath((string)fi.Path))
				{
					var folder = await ShellStorageFolder.FromPathAsync((string)fi.Path);
					var path = folder?.Path;

					// Fix for the Linux header
					if (path is not null && 
						(folderPaths.Contains(path) || (path.StartsWith(@"\\SHELL\") && folderPaths.Any(x => x.StartsWith(@"\\SHELL\")))))
					{
						await SafetyExtensions.IgnoreExceptions(async () =>
						{
							await fi.InvokeVerb("unpinfromhome");
						});
						continue;
					}
				}

				if (folderPaths.Contains((string)fi.Path))
				{
					await SafetyExtensions.IgnoreExceptions(async () =>
					{
						await fi.InvokeVerb("unpinfromhome");
					});
				}
			}

			await UpdateItemsWithExplorerAsync();

			if (invokeQuickAccessChangedEvent)
				UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, false));
		}

		/// <inheritdoc/>
		public bool IsPinnedToSidebar(string folderPath)
		{
			return PinnedFolders.Contains(folderPath);
		}

		/// <inheritdoc/>
		public async Task NotifyPinnedItemsChangesAsync(string[] items)
		{
			if (Equals(items, PinnedFolders.ToArray()))
				return;

			_quickAccessFolderWatcher.EnableRaisingEvents = false;

			// Unpin every item that is below this index and then pin them all in order
			await UnpinFromSidebarAsync([], false);

			await PinToSidebarAsync(items, false);
			_quickAccessFolderWatcher.EnableRaisingEvents = true;

			UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(items, true)
			{
				Reorder = true
			});
		}

		/// <inheritdoc/>
		public async Task UpdateItemsWithExplorerAsync()
		{
			await _addSyncSemaphore.WaitAsync();

			try
			{
				PinnedFolders = (await GetPinnedFoldersAsync())
					.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
					.Select(link => link.FilePath).ToList();

				RemoveStaleSidebarItems();

				await AddAllItemsToSidebarAsync();
			}
			finally
			{
				_addSyncSemaphore.Release();
			}
		}

		/// <inheritdoc/>
		public int IndexOfItem(INavigationControlItem locationItem)
		{
			lock (_PinnedFolderItems)
				return _PinnedFolderItems.FindIndex(x => x.Path == locationItem.Path);
		}

		/// <inheritdoc/>
		public async Task<LocationItem> CreateLocationItemFromPathAsync(string path)
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
			locationItem.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowProperties = true,
				ShowUnpinItem = true,
				ShowShellItems = true,
				ShowEmptyRecycleBin = string.Equals(path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase)
			};
			locationItem.IsDefaultLocation = false;
			locationItem.Text = res.Result?.DisplayName ?? SystemIO.Path.GetFileName(path.TrimEnd('\\'));

			if (res)
			{
				locationItem.IsInvalid = false;

				if (res && res.Result is not null)
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

				Debug.WriteLine($"Pinned item was invalid {res.ErrorCode}, item: {path}");
			}

			return locationItem;
		}

		/// <inheritdoc/>
		public async Task AddItemToSidebarAsync(string path)
		{
			var locationItem = await CreateLocationItemFromPathAsync(path);

			AddLocationItemToSidebar(locationItem);
		}

		/// <inheritdoc/>
		public async Task AddAllItemsToSidebarAsync()
		{
			if (UserSettingsService.GeneralSettingsService.ShowPinnedSection)
			{
				foreach (string path in PinnedFolders)
					await AddItemToSidebarAsync(path);
			}
		}

		/// <inheritdoc/>
		public void RemoveStaleSidebarItems()
		{
			// Remove unpinned items from PinnedFolderItems
			foreach (var childItem in PinnedFolderItems)
			{
				if (childItem is LocationItem item && !item.IsDefaultLocation && !PinnedFolders.Contains(item.Path))
				{
					lock (_PinnedFolderItems)
						_PinnedFolderItems.Remove(item);

					DataChanged?.Invoke(SectionType.Pinned, new(NotifyCollectionChangedAction.Remove, item));
				}
			}

			// Remove unpinned items from sidebar
			DataChanged?.Invoke(SectionType.Pinned, new(NotifyCollectionChangedAction.Reset));
		}

		/// <inheritdoc/>
		public async void LoadAsync(object? sender, SystemIO.FileSystemEventArgs e)
		{
			_quickAccessFolderWatcher.EnableRaisingEvents = false;

			await UpdateItemsWithExplorerAsync();

			UpdateQuickAccessWidget?.Invoke(null, new((await GetPinnedFoldersAsync()).ToArray(), true) { Reset = true });

			_quickAccessFolderWatcher.EnableRaisingEvents = true;
		}

		private void AddLocationItemToSidebar(LocationItem locationItem)
		{
			int insertIndex = -1;

			lock (_PinnedFolderItems)
			{
				if (_PinnedFolderItems.Any(x => x.Path == locationItem.Path))
					return;

				var lastItem = _PinnedFolderItems.LastOrDefault(x => x.ItemType is NavigationControlItemType.Location);

				insertIndex = lastItem is not null ? _PinnedFolderItems.IndexOf(lastItem) + 1 : 0;

				_PinnedFolderItems.Insert(insertIndex, locationItem);
			}

			DataChanged?.Invoke(SectionType.Pinned, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, locationItem, insertIndex));
		}

		private void PinnedItemsWatcher_Changed(object sender, SystemIO.FileSystemEventArgs e)
		{
			PinnedItemsModified?.Invoke(this, e);
		}
	}
}
