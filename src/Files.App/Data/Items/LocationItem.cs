// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.Data.Items
{
	public partial class LocationItem : ExpandableSidebarItemBase, INavigationControlItem, IExpandableSidebarFolder
	{
		public BitmapImage icon;
		public BitmapImage Icon
		{
			get => icon;
			set
			{
				SetProperty(ref icon, value, nameof(Icon));
				OnPropertyChanged(nameof(IconElement));
			}
		}

		public byte[] IconData { get; set; }

		private string text = "";
		public string Text
		{
			get => text;
			set
			{
				text = value;
				// Just in case path hasn't been set
				if (ToolTip is "")
					ToolTip = value;
			}
		}

		private string path;
		public string Path
		{
			get => path;
			set
			{
				path = value;
				ToolTip = string.IsNullOrEmpty(Path) ||
					Path.Contains('?', StringComparison.Ordinal) ||
					Path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) ||
					Path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase) ||
					Path == "Home" ||
					Path == "ReleaseNotes" ||
					Path == "Settings"
					? Text
					: Path;
			}
		}

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.Location;

		public bool IsDefaultLocation { get; set; }

		public object? Children
		{
			get
			{
				if (IsExpandableFolder)
					return ChildItems ??= [];
				if (Section == SectionType.Home)
					return null;
				return ChildItems;
			}
		}
		public BulkConcurrentObservableCollection<INavigationControlItem>? ChildItems { get; set; }

		protected override string ExpansionPath => path;
		protected override BulkConcurrentObservableCollection<INavigationControlItem> EnsureChildItems() => ChildItems ??= [];

		public IconElement? IconElement
		{
			get
			{
				var source = new ImageIconSource()
				{
					ImageSource = icon
				};
				return source.CreateIconElement();
			}
		}

		public bool SelectsOnInvoked { get; set; } = true;

		// Shared by LocationItem + DriveItem. Enumerates off the UI thread, dispatches a single AddRange (one CollectionChanged event instead of N), paints with the cached generic folder icon, then upgrades per-path icons in the background. onLoaded runs on the UI thread alongside the AddRange so callers can flip their loaded/unrealized flags atomically with the visual update.
		internal static async Task LoadSubfoldersIntoAsync(string enumerationPath, BulkConcurrentObservableCollection<INavigationControlItem> target, Action onLoaded)
		{
			try
			{
				var userSettings = Ioc.Default.GetService<IUserSettingsService>()?.FoldersSettingsService;
				var showHidden = userSettings?.ShowHiddenItems ?? false;
				var showProtected = userSettings?.ShowProtectedSystemFiles ?? false;
				var showDot = userSettings?.ShowDotFiles ?? false;

				var entries = await Task.Run(() => FolderHelpers.EnumerateSubfolders(enumerationPath, showHidden, showProtected, showDot));
				var iconBytes = await GetGenericSmallFolderIconBytesAsync();

				var dispatcher = MainWindow.Instance?.DispatcherQueue;
				if (dispatcher is null)
					return;

				List<LocationItem>? createdChildren = null;
				await dispatcher.EnqueueOrInvokeAsync(async () =>
				{
					BitmapImage? sharedIcon = null;
					if (iconBytes is not null)
					{
						try { sharedIcon = await iconBytes.ToBitmapAsync(); }
						// BitmapImage.SetSourceAsync throws on corrupt bytes; proceed without an icon.
						catch (Exception ex) { App.Logger?.LogDebug(ex, "Sidebar subfolder shared folder icon decode failed"); }
					}

					createdChildren = new List<LocationItem>(entries.Count);
					foreach (var entry in entries)
						createdChildren.Add(CreateSubfolder(entry, sharedIcon));

					target.AddRange(createdChildren);
					onLoaded();
				});

				if (createdChildren is not null)
					_ = UpgradeIconsAsync(createdChildren, iconBytes, dispatcher);
			}
			// FolderHelpers.EnumerateSubfolders / FileThumbnailHelper can throw UnauthorizedAccessException, IOException, or COMException on inaccessible / missing paths. Still run onLoaded on the dispatcher so the caller can clear HasUnrealizedChildren and mark childrenLoaded — otherwise the chevron stays and every subsequent click replays the failing enumeration.
			catch (Exception ex)
			{
				App.Logger?.LogDebug(ex, "Sidebar subfolder enumeration failed for {Path}", enumerationPath);
				await (MainWindow.Instance?.DispatcherQueue).EnqueueOrInvokeAsync(onLoaded);
			}
		}

		// Dummy-path trick (matches IconCacheService) makes the shell return the generic folder icon, not anything path-specific. Cached process-wide so every sidebar expansion reuses the same bytes.
		private static byte[]? cachedGenericSmallFolderIconBytes;
		private static readonly SemaphoreSlim genericSmallFolderIconLock = new(1, 1);
		private static readonly string genericFolderProbePath = System.IO.Path.Combine(System.IO.Path.GetPathRoot(Environment.SystemDirectory)!, "x46696c6573");

		internal static async Task<byte[]?> GetGenericSmallFolderIconBytesAsync()
		{
			if (cachedGenericSmallFolderIconBytes is not null)
				return cachedGenericSmallFolderIconBytes;

			await genericSmallFolderIconLock.WaitAsync();
			try
			{
				if (cachedGenericSmallFolderIconBytes is not null)
					return cachedGenericSmallFolderIconBytes;

				try
				{
					cachedGenericSmallFolderIconBytes = await FileThumbnailHelper.GetIconAsync(genericFolderProbePath, Constants.ShellIconSizes.Small, true, IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);
				}
				// FileThumbnailHelper.GetIconAsync can throw COMException when the shell handler is in a bad state; leave the cache null so children render without an icon.
				catch (Exception ex) { App.Logger?.LogDebug(ex, "LocationItem: generic small folder icon load failed"); }

				return cachedGenericSmallFolderIconBytes;
			}
			finally
			{
				genericSmallFolderIconLock.Release();
			}
		}

		// Replaces the shared generic folder icon with a per-path one only where the shell resolves a different image (custom desktop.ini icons, library covers, etc.). Identical bytes are skipped cheaply so the UI thread only pays for items that actually differ.
		internal static async Task UpgradeIconsAsync(List<LocationItem> children, byte[]? genericIconBytes, Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
		{
			foreach (var item in children)
			{
				var path = item.Path;
				if (string.IsNullOrEmpty(path))
					continue;

				byte[]? realBytes;
				try
				{
					// Size + scale must match the generic-icon fetch in LoadSubfoldersIntoAsync so the byte-equality skip below is valid.
					realBytes = await FileThumbnailHelper.GetIconAsync(path, Constants.ShellIconSizes.Small, true, IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);
				}
				// FileThumbnailHelper.GetIconAsync can throw COMException / UnauthorizedAccessException on inaccessible paths; keep the shared generic icon.
				catch (Exception ex)
				{
					App.Logger?.LogDebug(ex, "LocationItem: real icon load failed for {Path}", path);
					continue;
				}

				if (realBytes is null)
					continue;
				if (genericIconBytes is not null && realBytes.AsSpan().SequenceEqual(genericIconBytes))
					continue;

				await dispatcher.EnqueueOrInvokeAsync(async () =>
				{
					try
					{
						var bmp = await realBytes.ToBitmapAsync();
						if (bmp is not null)
							item.Icon = bmp;
					}
					// BitmapImage.SetSourceAsync throws on corrupt bytes; keep the generic icon.
					catch (Exception ex) { App.Logger?.LogDebug(ex, "LocationItem: real icon decode failed for {Path}", path); }
				}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
			}
		}

		internal static LocationItem CreateSubfolder(SubfolderEntry entry, BitmapImage? sharedIcon = null)
		{
			return new LocationItem
			{
				Path = entry.Path,
				Text = entry.Name,
				IsExpandableFolder = true,
				HasUnrealizedChildren = entry.HasSubfolders,
				IsHidden = entry.IsHidden,
				Icon = sharedIcon!,
				MenuOptions = new ContextMenuOptions
				{
					IsLocationItem = true,
					ShowProperties = true,
					ShowShellItems = true,
				},
			};
		}

		public bool IsHidden { get; set; }

		public bool IsInvalid { get; set; } = false;

		public bool IsPinned => App.QuickAccessManager.Model.PinnedFolders.Contains(path);

		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		public bool IsHeader { get; set; }

		private object toolTip = "";
		public virtual object ToolTip
		{
			get => toolTip;
			set
			{
				SetProperty(ref toolTip, value);
			}
		}

		public FrameworkElement? ItemDecorator => null;

		public int CompareTo(INavigationControlItem? other)
			=> Text.CompareTo(other.Text);

		public static T Create<T>() where T : LocationItem, new()
		{
			return new T();
		}
	}

	public sealed partial class RecycleBinLocationItem : LocationItem
	{
		private readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();

		public async void RefreshSpaceUsed(object? sender, FileSystemEventArgs e)
		{
			await RefreshSpaceUsedAsync();
		}

		private Task RefreshSpaceUsedAsync()
		{
			return MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				SpaceUsed = await Task.Run(() => StorageTrashBinService.GetSize());
			});
		}

		private ulong spaceUsed;
		public ulong SpaceUsed
		{
			get => spaceUsed;
			set
			{
				if (SetProperty(ref spaceUsed, value))
					OnPropertyChanged(nameof(ToolTip));
			}
		}

		public override object ToolTip
		{
			get => SpaceUsed.ToSizeString();
		}

		public RecycleBinLocationItem()
		{
			StorageTrashBinService.Watcher.ItemAdded += RefreshSpaceUsed;
			StorageTrashBinService.Watcher.ItemDeleted += RefreshSpaceUsed;

			_ = RefreshSpaceUsedAsync();
		}
	}
}
