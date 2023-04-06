using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.UserControls;
using Files.Backend.Services.Settings;
using Files.Sdk.Storage.LocatableStorage;
using Files.Shared.EventArguments;
using Files.Shared.Extensions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.ViewModels
{
	public class SidebarViewModel : ObservableObject, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

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
			get => UserSettingsService.PreferencesSettingsService.ShowFavoritesSection;
			set
			{
				if (value == UserSettingsService.PreferencesSettingsService.ShowFavoritesSection)
					return;

				UserSettingsService.PreferencesSettingsService.ShowFavoritesSection = value;
			}
		}

		public bool ShowLibrarySection
		{
			get => UserSettingsService.PreferencesSettingsService.ShowLibrarySection;
			set
			{
				if (value == UserSettingsService.PreferencesSettingsService.ShowLibrarySection)
					return;

				UserSettingsService.PreferencesSettingsService.ShowLibrarySection = value;
			}
		}

		public bool ShowDrivesSection
		{
			get => UserSettingsService.PreferencesSettingsService.ShowDrivesSection;
			set
			{
				if (value == UserSettingsService.PreferencesSettingsService.ShowDrivesSection)
					return;

				UserSettingsService.PreferencesSettingsService.ShowDrivesSection = value;
			}
		}

		public bool ShowCloudDrivesSection
		{
			get => UserSettingsService.PreferencesSettingsService.ShowCloudDrivesSection;
			set
			{
				if (value == UserSettingsService.PreferencesSettingsService.ShowCloudDrivesSection)
					return;

				UserSettingsService.PreferencesSettingsService.ShowCloudDrivesSection = value;
			}
		}

		public bool ShowNetworkDrivesSection
		{
			get => UserSettingsService.PreferencesSettingsService.ShowNetworkDrivesSection;
			set
			{
				if (value == UserSettingsService.PreferencesSettingsService.ShowNetworkDrivesSection)
					return;

				UserSettingsService.PreferencesSettingsService.ShowNetworkDrivesSection = value;
			}
		}

		public bool ShowWslSection
		{
			get => UserSettingsService.PreferencesSettingsService.ShowWslSection;
			set
			{
				if (value == UserSettingsService.PreferencesSettingsService.ShowWslSection)
					return;

				UserSettingsService.PreferencesSettingsService.ShowWslSection = value;
			}
		}

		public bool ShowFileTagsSection
		{
			get => UserSettingsService.PreferencesSettingsService.ShowFileTagsSection;
			set
			{
				if (value == UserSettingsService.PreferencesSettingsService.ShowFileTagsSection)
					return;

				UserSettingsService.PreferencesSettingsService.ShowFileTagsSection = value;
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
			CreateItemHome();

			Manager_DataChanged(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.CloudDrives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			App.QuickAccessManager.Model.DataChanged += Manager_DataChanged;
			App.LibraryManager.DataChanged += Manager_DataChanged;
			drivesViewModel.Drives.CollectionChanged += Manager_DataChanged;
			App.CloudDrivesManager.DataChanged += Manager_DataChanged;
			App.NetworkDrivesManager.DataChanged += Manager_DataChanged;
			App.WSLDistroManager.DataChanged += Manager_DataChanged;
			App.FileTagsManager.DataChanged += Manager_DataChanged;
		}

		private async void CreateItemHome()
		{
			await CreateSection(SectionType.Home);
		}

		private async void Manager_DataChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			await dispatcherQueue.EnqueueAsync(async () =>
			{
				var sectionType = (sender is SectionType s) ? s : SectionType.Drives;
				var section = await GetOrCreateSection(sectionType);
				Func<IReadOnlyList<INavigationControlItem>> getElements = () => sectionType switch
				{
					SectionType.Favorites => App.QuickAccessManager.Model.Favorites,
					SectionType.CloudDrives => App.CloudDrivesManager.Drives,
					SectionType.Drives => drivesViewModel.Drives.Cast<DriveItem>().ToList().AsReadOnly(),
					SectionType.Network => App.NetworkDrivesManager.Drives,
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
					section.Icon = await UIHelpers.GetIconResource(iconIdex);
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

		public async void UpdateSectionVisibility(SectionType sectionType, bool show)
		{
			if (show)
			{
				var preferencesSettingsService = UserSettingsService.PreferencesSettingsService;

				Func<Task> action = sectionType switch
				{
					SectionType.CloudDrives when preferencesSettingsService.ShowCloudDrivesSection => App.CloudDrivesManager.UpdateDrivesAsync,
					SectionType.Drives => drivesViewModel.UpdateDrivesAsync,
					SectionType.Network when preferencesSettingsService.ShowNetworkDrivesSection => App.NetworkDrivesManager.UpdateDrivesAsync,
					SectionType.WSL when preferencesSettingsService.ShowWslSection => App.WSLDistroManager.UpdateDrivesAsync,
					SectionType.FileTag when preferencesSettingsService.ShowFileTagsSection => App.FileTagsManager.UpdateFileTagsAsync,
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
				case nameof(UserSettingsService.PreferencesSettingsService.ShowFavoritesSection):
					UpdateSectionVisibility(SectionType.Favorites, ShowFavoritesSection);
					OnPropertyChanged(nameof(ShowFavoritesSection));
					break;
				case nameof(UserSettingsService.PreferencesSettingsService.ShowLibrarySection):
					UpdateSectionVisibility(SectionType.Library, ShowLibrarySection);
					OnPropertyChanged(nameof(ShowLibrarySection));
					break;
				case nameof(UserSettingsService.PreferencesSettingsService.ShowCloudDrivesSection):
					UpdateSectionVisibility(SectionType.CloudDrives, ShowCloudDrivesSection);
					OnPropertyChanged(nameof(ShowCloudDrivesSection));
					break;
				case nameof(UserSettingsService.PreferencesSettingsService.ShowDrivesSection):
					UpdateSectionVisibility(SectionType.Drives, ShowDrivesSection);
					OnPropertyChanged(nameof(ShowDrivesSection));
					break;
				case nameof(UserSettingsService.PreferencesSettingsService.ShowNetworkDrivesSection):
					UpdateSectionVisibility(SectionType.Network, ShowNetworkDrivesSection);
					OnPropertyChanged(nameof(ShowNetworkDrivesSection));
					break;
				case nameof(UserSettingsService.PreferencesSettingsService.ShowWslSection):
					UpdateSectionVisibility(SectionType.WSL, ShowWslSection);
					OnPropertyChanged(nameof(ShowWslSection));
					break;
				case nameof(UserSettingsService.PreferencesSettingsService.ShowFileTagsSection):
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
			drivesViewModel.Drives.CollectionChanged -= Manager_DataChanged;
			App.CloudDrivesManager.DataChanged -= Manager_DataChanged;
			App.NetworkDrivesManager.DataChanged -= Manager_DataChanged;
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
