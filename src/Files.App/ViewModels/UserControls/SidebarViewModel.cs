// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Files.App.UserControls.Sidebar;
using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls
{
	public class SidebarViewModel : ObservableObject, IDisposable, ISidebarViewModel
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

		private readonly NetworkDrivesViewModel networkDrivesViewModel = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();

		private IPaneHolder paneHolder;
		public IPaneHolder PaneHolder
		{
			get => paneHolder;
			set => SetProperty(ref paneHolder, value);
		}

		public MenuFlyout PaneFlyout;

		public IFilesystemHelpers FilesystemHelpers
			=> PaneHolder?.FilesystemHelpers;

		private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;
		private INavigationControlItem rightClickedItem;

		public BulkConcurrentObservableCollection<INavigationControlItem> SidebarItems { get; init; }
		public SidebarPinnedModel SidebarPinnedModel => App.QuickAccessManager.Model;
		public IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public static readonly GridLength CompactSidebarWidth = SidebarControl.GetSidebarCompactSize();

		private SidebarDisplayMode sidebarDisplayMode;
		public SidebarDisplayMode SidebarDisplayMode
		{
			get => sidebarDisplayMode;
			set
			{
				if (SetProperty(ref sidebarDisplayMode, value))
				{
					OnPropertyChanged(nameof(IsSidebarCompactSize));

					UpdateTabControlMargin();
				}
			}
		}

		private readonly SectionType[] SectionOrder =
			new SectionType[]
			{
				SectionType.Home,
				SectionType.Favorites,
				SectionType.Library,
				SectionType.Drives,
				SectionType.CloudDrives,
				SectionType.Network,
				SectionType.WSL,
				SectionType.FileTag
			};

		public bool IsSidebarCompactSize
			=> SidebarDisplayMode == SidebarDisplayMode.Compact || SidebarDisplayMode == SidebarDisplayMode.Minimal;

		public void NotifyInstanceRelatedPropertiesChanged(string arg)
		{
			UpdateSidebarSelectedItemFromArgs(arg);

			OnPropertyChanged(nameof(SidebarSelectedItem));
		}

		public void UpdateSidebarSelectedItemFromArgs(string arg)
		{
			var value = arg;

			INavigationControlItem? item = null;
			var sidebarItems = SidebarItems
				.Where(x => !string.IsNullOrWhiteSpace(x.Path))
				.Concat(SidebarItems.Where(x => (x as LocationItem)?.ChildItems is not null).SelectMany(x => ((LocationItem)x).ChildItems).Where(x => !string.IsNullOrWhiteSpace(x.Path)))
				.ToList();

			if (string.IsNullOrEmpty(value))
			{
				//SidebarSelectedItem = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home"));
				return;
			}

			item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
			item ??= sidebarItems.FirstOrDefault(x => x.Path.Equals(value + "\\", StringComparison.OrdinalIgnoreCase));
			item ??= sidebarItems.Where(x => value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase)).MaxBy(x => x.Path.Length);
			item ??= sidebarItems.FirstOrDefault(x => x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));

			if (item is null && value == "Home")
				item = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home"));

			if (SidebarSelectedItem != item)
				SidebarSelectedItem = item;

		}

		public bool IsSidebarOpen
		{
			get => UserSettingsService.AppearanceSettingsService.IsSidebarOpen;
			set
			{
				if (value == UserSettingsService.AppearanceSettingsService.IsSidebarOpen)
					return;

				UserSettingsService.AppearanceSettingsService.IsSidebarOpen = value;

				OnPropertyChanged();
			}
		}

		public bool ShowFavoritesSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowFavoritesSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowFavoritesSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowFavoritesSection = value;
			}
		}

		public bool ShowLibrarySection
		{
			get => UserSettingsService.GeneralSettingsService.ShowLibrarySection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowLibrarySection)
					return;

				UserSettingsService.GeneralSettingsService.ShowLibrarySection = value;
			}
		}

		public bool ShowDrivesSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowDrivesSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowDrivesSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowDrivesSection = value;
			}
		}

		public bool ShowCloudDrivesSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection = value;
			}
		}

		public bool ShowNetworkDrivesSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowNetworkDrivesSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowNetworkDrivesSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowNetworkDrivesSection = value;
			}
		}

		public bool ShowWslSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowWslSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowWslSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowWslSection = value;
			}
		}

		public bool ShowFileTagsSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowFileTagsSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowFileTagsSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowFileTagsSection = value;
			}
		}

		private INavigationControlItem selectedSidebarItem;

		public INavigationControlItem SidebarSelectedItem
		{
			get => selectedSidebarItem;
			set => SetProperty(ref selectedSidebarItem, value);
		}

		public SidebarViewModel()
		{
			dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

			SidebarItems = new BulkConcurrentObservableCollection<INavigationControlItem>();
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
			CreateItemHomeAsync();

			Manager_DataChanged(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.CloudDrives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			App.QuickAccessManager.Model.DataChanged += Manager_DataChanged;
			App.LibraryManager.DataChanged += Manager_DataChanged;
			drivesViewModel.Drives.CollectionChanged += (x, args) => Manager_DataChanged(SectionType.Drives, args);
			App.CloudDrivesManager.DataChanged += Manager_DataChanged;
			networkDrivesViewModel.Drives.CollectionChanged += (x, args) => Manager_DataChanged(SectionType.Network, args);
			App.WSLDistroManager.DataChanged += Manager_DataChanged;
			App.FileTagsManager.DataChanged += Manager_DataChanged;

			HideSectionCommand = new RelayCommand(HideSection);
			UnpinItemCommand = new RelayCommand(UnpinItem);
			PinItemCommand = new RelayCommand(PinItem);
			OpenInNewTabCommand = new AsyncRelayCommand(OpenInNewTab);
			OpenInNewWindowCommand = new AsyncRelayCommand(OpenInNewWindow);
			OpenInNewPaneCommand = new AsyncRelayCommand(OpenInNewPane);
			EjectDeviceCommand = new AsyncRelayCommand(EjectDevice);
			FormatDriveCommand = new RelayCommand(FormatDrive);
			OpenPropertiesCommand = new RelayCommand<CommandBarFlyout>(OpenProperties);
			ReorderItemsCommand = new AsyncRelayCommand(ReorderItems);
		}

		private Task CreateItemHomeAsync()
		{
			return CreateSection(SectionType.Home);
		}

		private async void Manager_DataChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				var sectionType = (SectionType)sender;
				var section = await GetOrCreateSection(sectionType);
				Func<IReadOnlyList<INavigationControlItem>> getElements = () => sectionType switch
				{
					SectionType.Favorites => App.QuickAccessManager.Model.Favorites,
					SectionType.CloudDrives => App.CloudDrivesManager.Drives,
					SectionType.Drives => drivesViewModel.Drives.Cast<DriveItem>().ToList().AsReadOnly(),
					SectionType.Network => networkDrivesViewModel.Drives.Cast<DriveItem>().ToList().AsReadOnly(),
					SectionType.WSL => App.WSLDistroManager.Distros,
					SectionType.Library => App.LibraryManager.Libraries,
					SectionType.FileTag => App.FileTagsManager.FileTags,
					_ => null
				};
				await SyncSidebarItems(section, getElements, e);
			});
		}

		private async Task SyncSidebarItems(LocationItem section, Func<IReadOnlyList<INavigationControlItem>> getElements, NotifyCollectionChangedEventArgs e)
		{
			if (section is null)
			{
				return;
			}

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					{
						for (int i = 0; i < e.NewItems.Count; i++)
						{
							var index = e.NewStartingIndex < 0 ? -1 : i + e.NewStartingIndex;
							await AddElementToSection((INavigationControlItem)e.NewItems[i], section, index);
						}

						break;
					}

				case NotifyCollectionChangedAction.Move:
				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Replace:
					{
						foreach (INavigationControlItem elem in e.OldItems)
						{
							var match = section.ChildItems.FirstOrDefault(x => x.Path == elem.Path);
							section.ChildItems.Remove(match);
						}
						if (e.Action != NotifyCollectionChangedAction.Remove)
						{
							goto case NotifyCollectionChangedAction.Add;
						}

						break;
					}

				case NotifyCollectionChangedAction.Reset:
					{
						foreach (INavigationControlItem elem in getElements())
						{
							await AddElementToSection(elem, section);
						}
						foreach (INavigationControlItem elem in section.ChildItems.ToList())
						{
							if (!getElements().Any(x => x.Path == elem.Path))
							{
								section.ChildItems.Remove(elem);
							}
						}

						break;
					}
			}
		}

		private bool IsLibraryOnSidebar(LibraryLocationItem item)
			=> item is not null && !item.IsEmpty && item.IsDefaultLocation;

		private async Task AddElementToSection(INavigationControlItem elem, LocationItem section, int index = -1)
		{
			if (elem is LibraryLocationItem lib)
			{
				if (IsLibraryOnSidebar(lib) &&
					await lib.CheckDefaultSaveFolderAccess() &&
					!section.ChildItems.Any(x => x.Path == lib.Path))
				{
					section.ChildItems.AddSorted(elem);
					await lib.LoadLibraryIcon();
				}
			}
			else if (elem is DriveItem drive)
			{
				if (section.Section is SectionType.Network or SectionType.CloudDrives)
				{
					// Already sorted
					if (!section.ChildItems.Any(x => x.Path == drive.Path))
					{
						section.ChildItems.Insert(index < 0 ? section.ChildItems.Count : Math.Min(index, section.ChildItems.Count), drive);
						await drive.LoadThumbnailAsync(true);
					}
				}
				else
				{
					string drivePath = drive.Path;
					IList<string> paths = section.ChildItems.Select(item => item.Path).ToList();

					if (!paths.Contains(drivePath))
					{
						paths.AddSorted(drivePath);
						int position = paths.IndexOf(drivePath);

						section.ChildItems.Insert(position, drive);
						await drive.LoadThumbnailAsync(true);
					}
				}
			}
			else
			{
				if (!section.ChildItems.Any(x => x.Path == elem.Path))
				{
					section.ChildItems.Insert(index < 0 ? section.ChildItems.Count : Math.Min(index, section.ChildItems.Count), elem);
				}
			}

			if (IsSidebarOpen)
			{
				// Restore expanded state when section has items
				section.IsExpanded = Ioc.Default.GetRequiredService<SettingsViewModel>().Get(section.Text == "SidebarFavorites".GetLocalizedResource(), $"section:{section.Text.Replace('\\', '_')}");
			}
		}

		private async Task<LocationItem> GetOrCreateSection(SectionType sectionType)
		{
			LocationItem? section = GetSection(sectionType) ?? await CreateSection(sectionType);
			return section;
		}

		private LocationItem? GetSection(SectionType sectionType)
		{
			return SidebarItems.FirstOrDefault(x => x.Section == sectionType) as LocationItem;
		}

		private async Task<LocationItem> CreateSection(SectionType sectionType)
		{
			LocationItem section = null;
			BitmapImage icon = null;
			int iconIdex = -1;

			switch (sectionType)
			{
				case SectionType.Home:
					{
						section = BuildSection("Home".GetLocalizedResource(), sectionType, new ContextMenuOptions { IsLocationItem = true }, true);
						section.Path = "Home";
						section.Icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.HomeIcon));
						section.IsHeader = true;

						break;
					}

				case SectionType.Favorites:
					{
						if (ShowFavoritesSection == false)
						{
							break;
						}

						section = BuildSection("SidebarFavorites".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.FavoritesIcon));
						section.IsHeader = true;

						break;
					}

				case SectionType.Library:
					{
						if (ShowLibrarySection == false)
						{
							break;
						}
						section = BuildSection("SidebarLibraries".GetLocalizedResource(), sectionType, new ContextMenuOptions { IsLibrariesHeader = true, ShowHideSection = true }, false);
						iconIdex = Constants.ImageRes.Libraries;
						section.IsHeader = true;

						break;
					}

				case SectionType.Drives:
					{
						if (ShowDrivesSection == false)
						{
							break;
						}
						section = BuildSection("Drives".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						iconIdex = Constants.ImageRes.ThisPC;
						section.IsHeader = true;

						break;
					}

				case SectionType.CloudDrives:
					{
						if (ShowCloudDrivesSection == false || App.CloudDrivesManager.Drives.Any() == false)
						{
							break;
						}
						section = BuildSection("SidebarCloudDrives".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.CloudDriveIcon));
						section.IsHeader = true;

						break;
					}

				case SectionType.Network:
					{
						if (!ShowNetworkDrivesSection)
						{
							break;
						}
						section = BuildSection("SidebarNetworkDrives".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						iconIdex = Constants.ImageRes.NetworkDrives;
						section.IsHeader = true;

						break;
					}

				case SectionType.WSL:
					{
						if (ShowWslSection == false || App.WSLDistroManager.Distros.Any() == false)
						{
							break;
						}
						section = BuildSection("WSL".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.WslIconsPaths.GenericIcon));
						section.IsHeader = true;

						break;
					}

				case SectionType.FileTag:
					{
						if (!ShowFileTagsSection)
						{
							break;
						}
						section = BuildSection("FileTags".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.FileTagsIcon));
						section.IsHeader = true;

						break;
					}
			}

			if (section is not null)
			{
				if (icon is not null)
				{
					section.Icon = icon;
				}

				AddSectionToSideBar(section);

				if (iconIdex != -1)
				{
					section.Icon = await UIHelpers.GetSidebarIconResource(iconIdex);
				}
			}

			return section;
		}

		private LocationItem BuildSection(string sectionName, SectionType sectionType, ContextMenuOptions options, bool selectsOnInvoked)
		{
			return new LocationItem()
			{
				Text = sectionName,
				Section = sectionType,
				MenuOptions = options,
				SelectsOnInvoked = selectsOnInvoked,
				ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
			};
		}

		private void AddSectionToSideBar(LocationItem section)
		{
			var index = SectionOrder.TakeWhile(x => x != section.Section).Select(x => SidebarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
			SidebarItems.Insert(Math.Min(index, SidebarItems.Count), section);
		}

		public async Task UpdateSectionVisibility(SectionType sectionType, bool show)
		{
			if (show)
			{
				var generalSettingsService = UserSettingsService.GeneralSettingsService;

				Func<Task> action = sectionType switch
				{
					SectionType.CloudDrives when generalSettingsService.ShowCloudDrivesSection => App.CloudDrivesManager.UpdateDrivesAsync,
					SectionType.Drives => drivesViewModel.UpdateDrivesAsync,
					SectionType.Network when generalSettingsService.ShowNetworkDrivesSection => networkDrivesViewModel.UpdateDrivesAsync,
					SectionType.WSL when generalSettingsService.ShowWslSection => App.WSLDistroManager.UpdateDrivesAsync,
					SectionType.FileTag when generalSettingsService.ShowFileTagsSection => App.FileTagsManager.UpdateFileTagsAsync,
					SectionType.Library => App.LibraryManager.UpdateLibrariesAsync,
					SectionType.Favorites => App.QuickAccessManager.Model.AddAllItemsToSidebar,
					_ => () => Task.CompletedTask
				};

				Manager_DataChanged(sectionType, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

				await action();
			}
			else
			{
				SidebarItems.Remove(SidebarItems.FirstOrDefault(x => x.Section == sectionType));
			}
		}

		private void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(UserSettingsService.AppearanceSettingsService.IsSidebarOpen):
					if (UserSettingsService.AppearanceSettingsService.IsSidebarOpen != IsSidebarOpen)
					{
						OnPropertyChanged(nameof(IsSidebarOpen));
					}
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowFavoritesSection):
					UpdateSectionVisibility(SectionType.Favorites, ShowFavoritesSection);
					OnPropertyChanged(nameof(ShowFavoritesSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowLibrarySection):
					UpdateSectionVisibility(SectionType.Library, ShowLibrarySection);
					OnPropertyChanged(nameof(ShowLibrarySection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection):
					UpdateSectionVisibility(SectionType.CloudDrives, ShowCloudDrivesSection);
					OnPropertyChanged(nameof(ShowCloudDrivesSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowDrivesSection):
					UpdateSectionVisibility(SectionType.Drives, ShowDrivesSection);
					OnPropertyChanged(nameof(ShowDrivesSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowNetworkDrivesSection):
					UpdateSectionVisibility(SectionType.Network, ShowNetworkDrivesSection);
					OnPropertyChanged(nameof(ShowNetworkDrivesSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowWslSection):
					UpdateSectionVisibility(SectionType.WSL, ShowWslSection);
					OnPropertyChanged(nameof(ShowWslSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowFileTagsSection):
					UpdateSectionVisibility(SectionType.FileTag, ShowFileTagsSection);
					OnPropertyChanged(nameof(ShowFileTagsSection));
					break;
			}
		}

		public void Dispose()
		{
			UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;

			App.QuickAccessManager.Model.DataChanged -= Manager_DataChanged;
			App.LibraryManager.DataChanged -= Manager_DataChanged;
			drivesViewModel.Drives.CollectionChanged -= (x, args) => Manager_DataChanged(SectionType.Drives, args);
			App.CloudDrivesManager.DataChanged -= Manager_DataChanged;
			networkDrivesViewModel.Drives.CollectionChanged -= (x, args) => Manager_DataChanged(SectionType.Network, args);
			App.WSLDistroManager.DataChanged -= Manager_DataChanged;
			App.FileTagsManager.DataChanged -= Manager_DataChanged;
		}

		public void UpdateTabControlMargin()
		{
			TabControlMargin = SidebarDisplayMode switch
			{
				// This prevents the pane toggle button from overlapping the tab control in minimal mode
				SidebarDisplayMode.Minimal => new GridLength(44, GridUnitType.Pixel),
				_ => new GridLength(0, GridUnitType.Pixel),
			};
		}

		public async void HandleItemDropped(ItemDroppedEventArgs args)
		{
			if (args.DropTarget is not LocationItem locationItem) return;

			if (Utils.Storage.FilesystemHelpers.HasDraggedStorageItems(args.DroppedItem))
			{
				var deferral = args.RawEvent.GetDeferral();
				if (string.IsNullOrEmpty(locationItem.Path) && SectionType.Favorites.Equals(locationItem.Section)) // Pin to Favorites section
				{
					var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);
					foreach (var item in storageItems)
					{
						if (item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.FavoriteItems.Contains(item.Path))
							QuickAccessService.PinToSidebar(item.Path);
					}
				}
				else
				{
					await FilesystemHelpers.PerformOperationTypeAsync(args.RawEvent.AcceptedOperation, args.DroppedItem, locationItem.Path, false, true);
				}
				deferral.Complete();
			}
		}

		public void HandleItemContextInvoked(object sender, ItemContextInvokedArgs args)

		{
			if (sender is not FrameworkElement sidebarItem) return;

			if (args.Item is not INavigationControlItem item)
			{
				// We are in the pane context requested path
				PaneFlyout.ShowAt(sender as FrameworkElement, args.Position);
				return;
			}

			rightClickedItem = item;
			var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;

			var menuItems = GetLocationItemMenuItems(item, itemContextMenuFlyout);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			secondaryElements.OfType<FrameworkElement>()
								.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			itemContextMenuFlyout.ShowAt(sidebarItem, new FlyoutShowOptions { Position = args.Position });

			if (item.MenuOptions.ShowShellItems)
				_ = ShellContextmenuHelper.LoadShellMenuItems(rightClickedItem.Path, itemContextMenuFlyout, item.MenuOptions);
		}

		public async void HandleItemInvoked(object item)
		{
			if (item is not INavigationControlItem navigationControlItem) return;
			var navigationPath = item as string;

			if (await DriveHelpers.CheckEmptyDrive(navigationPath))
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed && navigationPath is not null)
			{
				await NavigationHelpers.OpenPathInNewTab(navigationPath);
				return;
			}

			// Type of page to navigate
			Type? sourcePageType = null;

			switch (navigationControlItem.ItemType)
			{
				case NavigationControlItemType.Location:
					{
						// Get the path of the invoked item
						var ItemPath = navigationControlItem.Path;

						if (ItemPath is null)
							ItemPath = navigationControlItem.Text;

						// Home item
						if (ItemPath != null && ItemPath.Equals("Home", StringComparison.OrdinalIgnoreCase))
						{
							navigationPath = "Home";
							sourcePageType = typeof(HomePage);
						}
						else
						{
							navigationPath = navigationControlItem.Path;
						}
						break;
					}

				case NavigationControlItemType.FileTag:
					var tagPath = navigationControlItem.Path; // Get the path of the invoked item
					if (PaneHolder?.ActivePane is IShellPage shp)
					{
						shp.NavigateToPath(tagPath, new NavigationArguments()
						{
							IsSearchResultPage = true,
							SearchPathParam = "Home",
							SearchQuery = tagPath,
							AssociatedTabInstance = shp,
							NavPathParam = tagPath
						});
					}
					return;

				default:
					{
						navigationPath = navigationControlItem.Path;
						break;
					}
			}

			if (PaneHolder?.ActivePane is IShellPage shellPage)
				shellPage.NavigateToPath(navigationPath, sourcePageType);
		}

		public readonly ICommand CreateLibraryCommand = new AsyncRelayCommand(LibraryManager.ShowCreateNewLibraryDialog);

		public readonly ICommand RestoreLibrariesCommand = new AsyncRelayCommand(LibraryManager.ShowRestoreDefaultLibrariesDialog);

		private ICommand HideSectionCommand { get; }

		private ICommand PinItemCommand { get; }

		private ICommand UnpinItemCommand { get; }

		private ICommand OpenInNewTabCommand { get; }

		private ICommand OpenInNewWindowCommand { get; }

		private ICommand OpenInNewPaneCommand { get; }

		private ICommand EjectDeviceCommand { get; }

		private ICommand FormatDriveCommand { get; }

		private ICommand OpenPropertiesCommand { get; }

		private ICommand ReorderItemsCommand { get; }

		private async Task OpenInNewPane()
		{
			if (await DriveHelpers.CheckEmptyDrive(rightClickedItem.Path))
				return;
			PaneHolder.OpenPathInNewPane(rightClickedItem.Path);
		}

		private async Task OpenInNewTab()
		{
			if (await DriveHelpers.CheckEmptyDrive(rightClickedItem.Path))
				return;

			await NavigationHelpers.OpenPathInNewTab(rightClickedItem.Path);
		}

		private async Task OpenInNewWindow()
		{
			if (await DriveHelpers.CheckEmptyDrive(rightClickedItem.Path))
				return;

			await NavigationHelpers.OpenPathInNewWindowAsync(rightClickedItem.Path);
		}

		private void PinItem()
		{
			if (rightClickedItem is DriveItem)
				_ = QuickAccessService.PinToSidebar(new[] { rightClickedItem.Path });
		}
		private void UnpinItem()
		{
			if (rightClickedItem.Section == SectionType.Favorites || rightClickedItem is DriveItem)
				_ = QuickAccessService.UnpinFromSidebar(rightClickedItem.Path);
		}

		private void HideSection()
		{
			switch (rightClickedItem.Section)
			{
				case SectionType.Favorites:
					UserSettingsService.GeneralSettingsService.ShowFavoritesSection = false;
					break;
				case SectionType.Library:
					UserSettingsService.GeneralSettingsService.ShowLibrarySection = false;
					break;
				case SectionType.CloudDrives:
					UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection = false;
					break;
				case SectionType.Drives:
					UserSettingsService.GeneralSettingsService.ShowDrivesSection = false;
					break;
				case SectionType.Network:
					UserSettingsService.GeneralSettingsService.ShowNetworkDrivesSection = false;
					break;
				case SectionType.WSL:
					UserSettingsService.GeneralSettingsService.ShowWslSection = false;
					break;
				case SectionType.FileTag:
					UserSettingsService.GeneralSettingsService.ShowFileTagsSection = false;
					break;
			}
		}

		private async Task ReorderItems()
		{
			var dialog = new ReorderSidebarItemsDialogViewModel();
			var dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			var result = await dialogService.ShowDialogAsync(dialog);
		}

		private void OpenProperties(CommandBarFlyout menu)
		{
			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = (s, e) =>
			{
				menu.Closed -= flyoutClosed;
				if (rightClickedItem is DriveItem)
					FilePropertiesHelpers.OpenPropertiesWindow(rightClickedItem, PaneHolder.ActivePane);
				else if (rightClickedItem is LibraryLocationItem library)
					FilePropertiesHelpers.OpenPropertiesWindow(new LibraryItem(library), PaneHolder.ActivePane);
				else if (rightClickedItem is LocationItem locationItem)
				{
					ListedItem listedItem = new ListedItem(null!)
					{
						ItemPath = locationItem.Path,
						ItemNameRaw = locationItem.Text,
						PrimaryItemAttribute = StorageItemTypes.Folder,
						ItemType = "Folder".GetLocalizedResource(),
					};

					FilePropertiesHelpers.OpenPropertiesWindow(listedItem, PaneHolder.ActivePane);
				}
			};
			menu.Closed += flyoutClosed;
		}

		private async Task EjectDevice()
		{
			var result = await DriveHelpers.EjectDeviceAsync(rightClickedItem.Path);
			await UIHelpers.ShowDeviceEjectResultAsync(rightClickedItem is DriveItem driveItem ? driveItem.Type : Data.Items.DriveType.Unknown, result);
		}

		private void FormatDrive()
		{
			Win32API.OpenFormatDriveDialog(rightClickedItem.Path);
		}

		private List<ContextMenuFlyoutItemViewModel> GetLocationItemMenuItems(INavigationControlItem item, CommandBarFlyout menu)
		{
			var options = item.MenuOptions;

			var favoriteModel = App.QuickAccessManager.Model;
			var favoriteIndex = favoriteModel.IndexOfItem(item);
			var favoriteCount = favoriteModel.FavoriteItems.Count;

			var isFavoriteItem = item.Section is SectionType.Favorites && favoriteIndex is not -1;
			var showMoveItemUp = isFavoriteItem && favoriteIndex > 0;
			var showMoveItemDown = isFavoriteItem && favoriteIndex < favoriteCount - 1;

			var isDriveItem = item is DriveItem;
			var isDriveItemPinned = isDriveItem && ((DriveItem)item).IsPinned;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarCreateNewLibrary/Text".GetLocalizedResource(),
					Glyph = "\uE710",
					Command = CreateLibraryCommand,
					ShowItem = options.IsLibrariesHeader
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarRestoreLibraries/Text".GetLocalizedResource(),
					Glyph = "\uE10E",
					Command = RestoreLibrariesCommand,
					ShowItem = options.IsLibrariesHeader
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.EmptyRecycleBin)
				{
					IsVisible = options.ShowEmptyRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.RestoreAllRecycleBin)
				{
					IsVisible = options.ShowEmptyRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand,
					ShowItem = options.IsLocationItem && UserSettingsService.GeneralSettingsService.ShowOpenInNewTab
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow",
					},
					Command = OpenInNewWindowCommand,
					ShowItem = options.IsLocationItem && UserSettingsService.GeneralSettingsService.ShowOpenInNewTab
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					ShowItem = options.IsLocationItem && UserSettingsService.GeneralSettingsService.ShowOpenInNewPane
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "PinToFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconPinToFavorites",
					},
					Command = PinItemCommand,
					ShowItem = isDriveItem && !isDriveItemPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconUnpinFromFavorites",
					},
					Command = UnpinItemCommand,
					ShowItem = options.ShowUnpinItem || isDriveItemPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ReorderSidebarItemsDialogText".GetLocalizedResource(),
					Glyph = "\uE8D8",
					Command = ReorderItemsCommand,
					ShowItem = isFavoriteItem || item.Section is SectionType.Favorites
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = string.Format("SideBarHideSectionFromSideBar/Text".GetLocalizedResource(), rightClickedItem.Text),
					Glyph = "\uE77A",
					Command = HideSectionCommand,
					ShowItem = options.ShowHideSection
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarEjectDevice/Text".GetLocalizedResource(),
					Command = EjectDeviceCommand,
					ShowItem = options.ShowEjectDevice
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "FormatDriveText".GetLocalizedResource(),
					Command = FormatDriveCommand,
					CommandParameter = item,
					ShowItem = options.ShowFormatDrive
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand,
					CommandParameter = menu,
					ShowItem = options.ShowProperties
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
					IsHidden = !options.ShowShellItems,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
					IsHidden = !options.ShowShellItems,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		public async void HandleItemDragOver(ItemDragOverEventArgs args)
		{
			if (args.DropTarget is not LocationItem locationItem) return;
			
			var rawEvent = args.RawEvent;
			var deferral = rawEvent.GetDeferral();

			if (Utils.Storage.FilesystemHelpers.HasDraggedStorageItems(args.DroppedItem))
			{
				args.RawEvent.Handled = true;

				var isPathNull = string.IsNullOrEmpty(locationItem.Path);
				var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);
				var hasStorageItems = storageItems.Any();

				if (isPathNull && hasStorageItems && SectionType.Favorites.Equals(locationItem.Section))
				{
					var haveFoldersToPin = storageItems.Any(item => item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.FavoriteItems.Contains(item.Path));

					if (!haveFoldersToPin)
					{
						rawEvent.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						var captionText = "PinToFavorites".GetLocalizedResource();
						CompleteDragEventArgs(rawEvent, captionText, DataPackageOperation.Move);
					}
				}
				else if (isPathNull ||
					(hasStorageItems && storageItems.AreItemsAlreadyInFolder(locationItem.Path)) ||
					locationItem.Path.StartsWith("Home", StringComparison.OrdinalIgnoreCase))
				{
					rawEvent.AcceptedOperation = DataPackageOperation.None;
				}
				else if (hasStorageItems is false)
				{
					rawEvent.AcceptedOperation = DataPackageOperation.None;
				}
				else
				{
					string captionText;
					DataPackageOperation operationType;
					if (locationItem.Path.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
					{
						captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Move;
					}
					else if (rawEvent.Modifiers.HasFlag(DragDropModifiers.Alt) || rawEvent.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
					{
						captionText = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Link;
					}
					else if (rawEvent.Modifiers.HasFlag(DragDropModifiers.Control))
					{
						captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Copy;
					}
					else if (rawEvent.Modifiers.HasFlag(DragDropModifiers.Shift))
					{
						captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Move;
					}
					else if (storageItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
						|| ZipStorageFolder.IsZipPath(locationItem.Path))
					{
						captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Copy;
					}
					else if (locationItem.IsDefaultLocation || storageItems.AreItemsInSameDrive(locationItem.Path))
					{
						captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Move;
					}
					else
					{
						captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Copy;
					}
					CompleteDragEventArgs(rawEvent, captionText, operationType);
				}
			}

			deferral.Complete();
		}

		private static DragEventArgs CompleteDragEventArgs(DragEventArgs e, string captionText, DataPackageOperation operationType)
		{
			e.DragUIOverride.IsCaptionVisible = true;
			e.DragUIOverride.Caption = captionText;
			e.AcceptedOperation = operationType;
			return e;
		}

		private GridLength tabControlMargin;
		public GridLength TabControlMargin
		{
			get => tabControlMargin;
			set => SetProperty(ref tabControlMargin, value);
		}
	}
}
