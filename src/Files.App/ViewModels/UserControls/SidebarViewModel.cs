// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI;
using Files.App.Data.Items;
using Files.App.UserControls;
using Files.Shared.EventArguments;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;
using System.IO;
using static Files.App.Constants.Widgets;

namespace Files.App.ViewModels.UserControls
{
	public class SidebarViewModel : ObservableObject, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private readonly NetworkDrivesViewModel networkDrivesViewModel = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();

		private IPaneHolder paneHolder;
		public IPaneHolder PaneHolder
		{
			get => paneHolder;
			set => SetProperty(ref paneHolder, value);
		}

		public IFilesystemHelpers FilesystemHelpers
			=> PaneHolder?.FilesystemHelpers;

		private DispatcherQueue dispatcherQueue;

		public BulkConcurrentObservableCollection<INavigationControlItem> SideBarItems { get; init; }

		public static readonly GridLength CompactSidebarWidth = SidebarControl.GetSidebarCompactSize();

		private NavigationViewDisplayMode sidebarDisplayMode;
		public NavigationViewDisplayMode SidebarDisplayMode
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

		private readonly SectionType[] SectionOrder = new SectionType[] {
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
			=> SidebarDisplayMode == NavigationViewDisplayMode.Compact || SidebarDisplayMode == NavigationViewDisplayMode.Minimal;

		public void NotifyInstanceRelatedPropertiesChanged(string arg)
		{
			UpdateSidebarSelectedItemFromArgs(arg);

			OnPropertyChanged(nameof(SidebarSelectedItem));
		}

		public void UpdateSidebarSelectedItemFromArgs(string arg)
		{
			var value = arg;

			INavigationControlItem? item = null;
			var sidebarItems = SideBarItems
				.Where(x => !string.IsNullOrWhiteSpace(x.Path))
				.Concat(SideBarItems.Where(x => (x as LocationItem)?.ChildItems is not null).SelectMany(x => ((LocationItem)x).ChildItems).Where(x => !string.IsNullOrWhiteSpace(x.Path)))
				.ToList();

			if (string.IsNullOrEmpty(value))
			{
				//SidebarSelectedItem = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home"));
				return;
			}

			item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
			item ??= sidebarItems.FirstOrDefault(x => x.Path.Equals(value + "\\", StringComparison.OrdinalIgnoreCase));
			item ??= sidebarItems.FirstOrDefault(x => value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));
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
			dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			SideBarItems = new BulkConcurrentObservableCollection<INavigationControlItem>();
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
			return SideBarItems.FirstOrDefault(x => x.Section == sectionType) as LocationItem;
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
			var index = SectionOrder.TakeWhile(x => x != section.Section).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
			SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
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
				SideBarItems.Remove(SideBarItems.FirstOrDefault(x => x.Section == sectionType));
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

		public void SidebarControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
		{
			SidebarDisplayMode = args.DisplayMode;
		}

		public void UpdateTabControlMargin()
		{
			TabControlMargin = SidebarDisplayMode switch
			{
				// This prevents the pane toggle button from overlapping the tab control in minimal mode
				NavigationViewDisplayMode.Minimal => new GridLength(44, GridUnitType.Pixel),
				_ => new GridLength(0, GridUnitType.Pixel),
			};
		}

		private GridLength tabControlMargin;

		public GridLength TabControlMargin
		{
			get => tabControlMargin;
			set => SetProperty(ref tabControlMargin, value);
		}
	}
}
