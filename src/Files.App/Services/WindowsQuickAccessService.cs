// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using System.Collections.Specialized;

namespace Files.App.Services
{
	internal sealed class WindowsQuickAccessService : IWindowsQuickAccessService
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Constants

		private const string ShellContextMenuVerbPinToHome = "PinToHome";

		private const string ShellContextMenuVerbUnpinToHome = "UnpinFromHome";

		private const string ShellPropertyIsPinned = "System.Home.IsPinned";

		private const string ShellMemberNameSpace = "NameSpace";

		private const string ShellProgIDApplication = "Shell.Application";

		// Fields

		private readonly SemaphoreSlim _addSyncSemaphore = new(1, 1);

		private readonly SystemIO.FileSystemWatcher _quickAccessFolderWatcher;

		// Properties

		/// <inheritdoc/>
		public List<string> PinnedFolderPaths { get; set; } = [];

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
		public event EventHandler<ModifyQuickAccessEventArgs>? PinnedItemsChanged;

		/// <summary>
		/// Initializes an instance of <see cref="WindowsQuickAccessService"/>.
		/// </summary>
		public WindowsQuickAccessService()
		{
			_quickAccessFolderWatcher = new()
			{
				Path = SystemIO.Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent", "AutomaticDestinations"),
				Filter = "f01b4d95cf55d32a.automaticDestinations-ms",
				NotifyFilter = SystemIO.NotifyFilters.LastAccess | SystemIO.NotifyFilters.LastWrite | SystemIO.NotifyFilters.FileName,
				EnableRaisingEvents = true,
			};

			_quickAccessFolderWatcher.Changed += async (s, e) =>
			{
				_quickAccessFolderWatcher.EnableRaisingEvents = false;

				await UpdatePinnedFolders();
				PinnedItemsChanged?.Invoke(null, new((await GetPinnedFoldersAsync()).ToArray(), true) { Reset = true });

				_quickAccessFolderWatcher.EnableRaisingEvents = true;
			};
		}

		/// <inheritdoc/>
		public async Task InitializeAsync()
		{
			// Pin RecycleBin folder
			if (!PinnedFolderPaths.Contains(Constants.UserEnvironmentPaths.RecycleBinPath) && SystemInformation.Instance.IsFirstRun)
				await PinFolderToSidebarAsync([Constants.UserEnvironmentPaths.RecycleBinPath]);

			// Refresh
			await UpdatePinnedFolders();
		}

		/// <inheritdoc/>
		public async Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync()
		{
			// TODO: Return IAsyncEnumerable, instead
			return (await Win32Helper.GetShellFolderAsync(Constants.CLID.QuickAccess, false, true, 0, int.MaxValue, "System.Home.IsPinned"))
				.Enumerate
				.Where(link => link.IsFolder);
		}

		/// <inheritdoc/>
		public async Task PinFolderToSidebarAsync(string[] folderPaths, bool invokeQuickAccessChangedEvent = true)
		{
			foreach (string folderPath in folderPaths)
				await ContextMenu.InvokeVerb(ShellContextMenuVerbPinToHome, [folderPath]);

			await UpdatePinnedFolders();

			if (invokeQuickAccessChangedEvent)
				PinnedItemsChanged?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, true));
		}

		/// <inheritdoc/>
		public async Task UnpinFolderFromSidebarAsync(string[] folderPaths, bool invokeQuickAccessChangedEvent = true)
		{
			// Get the shell application Program ID
			var shellAppType = Type.GetTypeFromProgID(ShellProgIDApplication)!;

			// Create shell instance
			var shell = Activator.CreateInstance(shellAppType);

			// Get the QuickAccess shell folder contents
			dynamic? shellItems =
				shellAppType.InvokeMember(
					ShellMemberNameSpace,
					System.Reflection.BindingFlags.InvokeMethod,
					null,
					shell,
					[$"Shell:{Constants.CLID.QuickAccess}"]);

			if (shellItems is null)
				return;

			if (folderPaths.Length == 0)
			{
				folderPaths = (await GetPinnedFoldersAsync())
					.Where(link => (bool?)link.Properties[ShellPropertyIsPinned] ?? false)
					.Select(link => link.FilePath).ToArray();
			}

			foreach (var shellItem in shellItems.Items())
			{
				if (ShellStorageFolder.IsShellPath((string)shellItem.Path))
				{
					//var folder = await ShellStorageFolder.FromPathAsync((string)shellItem.Path);
					var path = (string)shellItem.Path;

					if (path is not null &&
						(folderPaths.Contains(path) || (path.StartsWith(@"\\SHELL\") && folderPaths.Any(x => x.StartsWith(@"\\SHELL\")))))
					{
						// Unpin
						await SafetyExtensions.IgnoreExceptions(async () =>
						{
							await shellItem.InvokeVerb(ShellContextMenuVerbUnpinToHome);
						});

						continue;
					}
				}

				if (folderPaths.Contains((string)shellItem.Path))
				{
					// Unpin
					await SafetyExtensions.IgnoreExceptions(async () =>
					{
						await shellItem.InvokeVerb(ShellContextMenuVerbUnpinToHome);
					});
				}
			}

			await UpdatePinnedFolders();

			if (invokeQuickAccessChangedEvent)
				PinnedItemsChanged?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, false));
		}

		/// <inheritdoc/>
		public bool IsPinnedFolder(string folderPath)
		{
			return PinnedFolderPaths.Contains(folderPath);
		}

		/// <inheritdoc/>
		public async Task RefreshPinnedFolders(string[] items)
		{
			if (Equals(items, PinnedFolderPaths.ToArray()))
				return;

			_quickAccessFolderWatcher.EnableRaisingEvents = false;

			// Unpin every item and pin new items in order
			await UnpinFolderFromSidebarAsync([], false);
			await PinFolderToSidebarAsync(items, false);

			_quickAccessFolderWatcher.EnableRaisingEvents = true;

			PinnedItemsChanged?.Invoke(this, new(items, true) { Reorder = true });
		}

		/// <inheritdoc/>
		public async Task UpdatePinnedFolders()
		{
			await _addSyncSemaphore.WaitAsync();

			try
			{
				PinnedFolderPaths = (await GetPinnedFoldersAsync())
					.Where(x => (bool?)x.Properties[ShellPropertyIsPinned] ?? false)
					.Select(x => x.FilePath).ToList();

				// Sync pinned items
				foreach (var childItem in PinnedFolderItems)
				{
					if (childItem is LocationItem item && !item.IsDefaultLocation && !PinnedFolderPaths.Contains(item.Path))
					{
						lock (_PinnedFolderItems)
							_PinnedFolderItems.Remove(item);

						DataChanged?.Invoke(SectionType.Pinned, new(NotifyCollectionChangedAction.Remove, item));
					}
				}

				DataChanged?.Invoke(SectionType.Pinned, new(NotifyCollectionChangedAction.Reset));

				await SyncPinnedItemsAsync();
			}
			finally
			{
				_addSyncSemaphore.Release();
			}
		}

		/// <inheritdoc/>
		public async Task SyncPinnedItemsAsync()
		{
			foreach (string path in PinnedFolderPaths)
			{
				var locationItem = await LocationItem.CreateLocationItemFromPathAsync(path);

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
		}

		/// <inheritdoc/>
		public int IndexOf(string path)
		{
			lock (_PinnedFolderItems)
				return _PinnedFolderItems.FindIndex(x => x.Path == path);
		}
	}
}
