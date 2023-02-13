using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Files.Shared.Extensions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.ViewModels
{
	[SupportedOSPlatform("Windows10.0.17763")]
	public class SidebarViewModel : ObservableObject, ISidebarViewModel
	{
		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private readonly DispatcherQueue dispatcherQueue;

		private readonly ImmutableArray<SectionType> sectionOrder = new SectionType[]
		{
			SectionType.Home,
			SectionType.Favorites,
			SectionType.Library,
			SectionType.Drives,
			SectionType.CloudDrives,
			SectionType.Network,
			SectionType.WSL,
			SectionType.FileTag,
		}.ToImmutableArray();

		private IPaneHolder? paneHolder;
		public IPaneHolder PaneHolder
		{
			get => paneHolder!;
			set => SetProperty(ref paneHolder, value);
		}

		public ICommand EmptyRecycleBinCommand { get; }

		private NavigationViewDisplayMode sidebarDisplayMode;
		public NavigationViewDisplayMode SidebarDisplayMode
		{
			get => sidebarDisplayMode;
			set
			{
				if (SetProperty(ref sidebarDisplayMode, value))
					OnPropertyChanged(nameof(IsSidebarCompactSize));
			}
		}

		private INavigationControlItem? selectedSidebarItem;
		public INavigationControlItem? SidebarSelectedItem
		{
			get => selectedSidebarItem;
			set => SetProperty(ref selectedSidebarItem, value);
		}

		ICollection<INavigationControlItem> ISidebarViewModel.SideBarItems => SideBarItems;
		public BulkConcurrentObservableCollection<INavigationControlItem> SideBarItems { get; }

		public bool IsSidebarCompactSize
			=> SidebarDisplayMode is NavigationViewDisplayMode.Compact or NavigationViewDisplayMode.Minimal;
		public bool IsSidebarOpen
		{
			get => userSettingsService.AppearanceSettingsService.IsSidebarOpen;
			set => userSettingsService.AppearanceSettingsService.IsSidebarOpen = value;
		}
		public bool ShowFavoritesSection
		{
			get => userSettingsService.AppearanceSettingsService.ShowFavoritesSection;
			set => userSettingsService.AppearanceSettingsService.ShowFavoritesSection = value;
		}
		public bool ShowLibrarySection
		{
			get => userSettingsService.AppearanceSettingsService.ShowLibrarySection;
			set => userSettingsService.AppearanceSettingsService.ShowLibrarySection = value;
		}
		public bool ShowDrivesSection
		{
			get => userSettingsService.AppearanceSettingsService.ShowDrivesSection;
			set => userSettingsService.AppearanceSettingsService.ShowDrivesSection = value;
		}
		public bool ShowCloudDrivesSection
		{
			get => userSettingsService.AppearanceSettingsService.ShowCloudDrivesSection;
			set => userSettingsService.AppearanceSettingsService.ShowCloudDrivesSection = value;
		}
		public bool ShowNetworkDrivesSection
		{
			get => userSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection;
			set => userSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection = value;
		}
		public bool ShowWslSection
		{
			get => userSettingsService.AppearanceSettingsService.ShowWslSection;
			set => userSettingsService.AppearanceSettingsService.ShowWslSection = value;
		}
		public bool ShowFileTagsSection
		{
			get => userSettingsService.AppearanceSettingsService.ShowFileTagsSection;
			set => userSettingsService.AppearanceSettingsService.ShowFileTagsSection = value;
		}

		public SidebarViewModel()
		{
			dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			SideBarItems = new BulkConcurrentObservableCollection<INavigationControlItem>();
			EmptyRecycleBinCommand = new AsyncRelayCommand(RecycleBinHelpers.S_EmptyRecycleBin);
			userSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
			CreateItemHome();

			var resetEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
			Manager_DataChanged(SectionType.Favorites, resetEventArgs);
			Manager_DataChanged(SectionType.Library, resetEventArgs);
			Manager_DataChanged(SectionType.Drives, resetEventArgs);
			Manager_DataChanged(SectionType.CloudDrives, resetEventArgs);
			Manager_DataChanged(SectionType.Network, resetEventArgs);
			Manager_DataChanged(SectionType.WSL, resetEventArgs);
			Manager_DataChanged(SectionType.FileTag, resetEventArgs);

			App.SidebarPinnedController.DataChanged += Manager_DataChanged;
			App.LibraryManager.DataChanged += Manager_DataChanged;
			App.DrivesManager.DataChanged += Manager_DataChanged;
			App.CloudDrivesManager.DataChanged += Manager_DataChanged;
			App.NetworkDrivesManager.DataChanged += Manager_DataChanged;
			App.WSLDistroManager.DataChanged += Manager_DataChanged;
			App.FileTagsManager.DataChanged += Manager_DataChanged;
		}

		public void UpdateSidebarSelectedItemFromArgs(string arg)
		{
			if (string.IsNullOrEmpty(arg))
				return;

			INavigationControlItem? item = null;
			var sidebarItems = SideBarItems
				.Where(x => !string.IsNullOrWhiteSpace(x.Path))
				.Concat(SideBarItems
					.Where(x => (x as LocationItem)?.ChildItems is not null)
					.SelectMany(x => ((LocationItem)x).ChildItems)
					.Where(x => !string.IsNullOrWhiteSpace(x.Path))
				)
				.ToList();

			item = sidebarItems.FirstOrDefault(x => x.Path.Equals(arg, StringComparison.OrdinalIgnoreCase))
				?? sidebarItems.FirstOrDefault(x => x.Path.Equals(arg + "\\", StringComparison.OrdinalIgnoreCase))
				?? sidebarItems.FirstOrDefault(x => arg.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase))
				?? sidebarItems.FirstOrDefault(x => x.Path.Equals(Path.GetPathRoot(arg), StringComparison.OrdinalIgnoreCase));

			if (item is null && arg == "Home".GetLocalizedResource())
				item = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home".GetLocalizedResource()));

			if (SidebarSelectedItem != item)
				SidebarSelectedItem = item;
		}

		public void NotifyInstanceRelatedPropertiesChanged(string arg)
		{
			UpdateSidebarSelectedItemFromArgs(arg);
			OnPropertyChanged(nameof(SidebarSelectedItem));
		}

		public void SidebarControl_DisplayModeChanged(NavigationView _, NavigationViewDisplayModeChangedEventArgs e)
			=> SidebarDisplayMode = e.DisplayMode;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool _)
		{
			userSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;

			App.SidebarPinnedController.DataChanged -= Manager_DataChanged;
			App.LibraryManager.DataChanged -= Manager_DataChanged;
			App.DrivesManager.DataChanged -= Manager_DataChanged;
			App.CloudDrivesManager.DataChanged -= Manager_DataChanged;
			App.NetworkDrivesManager.DataChanged -= Manager_DataChanged;
			App.WSLDistroManager.DataChanged -= Manager_DataChanged;
			App.FileTagsManager.DataChanged -= Manager_DataChanged;
		}

		private async void CreateItemHome() => await CreateSection(SectionType.Home);

		private async Task SyncSidebarItems(LocationItem section, Func<IReadOnlyList<INavigationControlItem>> getElements, NotifyCollectionChangedEventArgs e)
		{
			if (section is null)
				return;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems is null)
						break;
					for (int i = 0; i < e.NewItems.Count; i++)
					{
						var index = e.NewStartingIndex < 0 ? -1 : i + e.NewStartingIndex;
						await AddElementToSection((INavigationControlItem)e.NewItems[i]!, section, index);
					}
					break;
				case NotifyCollectionChangedAction.Move:
				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Replace:
					if (e.OldItems is null)
						break;
					foreach (INavigationControlItem elem in e.OldItems)
					{
						var match = section.ChildItems.FirstOrDefault(x => x.Path == elem.Path);
						section.ChildItems.Remove(match);
					}
					if (e.Action is not NotifyCollectionChangedAction.Remove)
						goto case NotifyCollectionChangedAction.Add;
					break;
				case NotifyCollectionChangedAction.Reset:
					foreach (INavigationControlItem elem in getElements())
						await AddElementToSection(elem, section);
					foreach (INavigationControlItem elem in section.ChildItems.ToList())
						if (!getElements().Any(x => x.Path == elem.Path))
							section.ChildItems.Remove(elem);
					break;
			}
		}

		private static bool IsLibraryOnSidebar(LibraryLocationItem item)
			=> item is not null && !item.IsEmpty && item.IsDefaultLocation;

		private async Task AddElementToSection(INavigationControlItem elem, LocationItem section, int index = -1)
		{
			if (elem is LibraryLocationItem lib)
			{
				if (IsLibraryOnSidebar(lib) && await lib.CheckDefaultSaveFolderAccess() && !section.ChildItems.Any(x => x.Path == lib.Path))
				{
					lib.Font = App.AppModel.SymbolFontFamily;
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
					await drive.LoadDriveIcon();
				}
			}
			else if (!section.ChildItems.Any(x => x.Path == elem.Path))
				section.ChildItems.Insert(index < 0 ? section.ChildItems.Count : Math.Min(index, section.ChildItems.Count), elem);

			if (IsSidebarOpen)
			{
				// Restore expanded state when section has items
				section.IsExpanded = App.AppSettings
					.Get(section.Text == "SidebarFavorites".GetLocalizedResource(), $"section:{section.Text.Replace('\\', '_')}");
			}
		}

		private LocationItem? GetSection(SectionType sectionType)
			=> SideBarItems.FirstOrDefault(x => x.Section == sectionType) as LocationItem;

		private async Task<LocationItem?> GetOrCreateSection(SectionType sectionType)
			=> GetSection(sectionType) ?? await CreateSection(sectionType);

		private async Task<LocationItem?> CreateSection(SectionType sectionType)
		{
			LocationItem? section = null;
			BitmapImage? icon = null;
			int iconIdex = -1;

			switch (sectionType)
			{
				case SectionType.Home:
					section = BuildSection("Home".GetLocalizedResource(),
						sectionType, new ContextMenuOptions { IsLocationItem = true }, true);
					section.Path = "Home".GetLocalizedResource();
					section.Font = App.AppModel.SymbolFontFamily;
					section.Icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.HomeIcon));
					break;
				case SectionType.Favorites:
					if (!ShowFavoritesSection)
						break;
					section = BuildSection("SidebarFavorites".GetLocalizedResource(),
						sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
					section.Font = App.AppModel.SymbolFontFamily;
					icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.FavoritesIcon));
					break;
				case SectionType.Library:
					if (!ShowLibrarySection)
						break;
					section = BuildSection("SidebarLibraries".GetLocalizedResource(),
						sectionType, new ContextMenuOptions { IsLibrariesHeader = true, ShowHideSection = true }, false);
					iconIdex = Constants.ImageRes.Libraries;
					break;
				case SectionType.Drives:
					if (!ShowDrivesSection)
						break;
					section = BuildSection("Drives".GetLocalizedResource(),
						sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
					iconIdex = Constants.ImageRes.ThisPC;
					break;
				case SectionType.CloudDrives:
					if (!ShowCloudDrivesSection || !App.CloudDrivesManager.Drives.Any())
						break;
					section = BuildSection("SidebarCloudDrives".GetLocalizedResource(),
						sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
					icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.CloudDriveIcon));
					break;
				case SectionType.Network:
					if (!ShowNetworkDrivesSection)
						break;
					section = BuildSection("SidebarNetworkDrives".GetLocalizedResource(),
						sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
					iconIdex = Constants.ImageRes.NetworkDrives;
					break;
				case SectionType.WSL:
					if (!ShowWslSection || !App.WSLDistroManager.Distros.Any())
						break;
					section = BuildSection("WSL".GetLocalizedResource(),
						sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
					icon = new BitmapImage(new Uri(Constants.WslIconsPaths.GenericIcon));
					break;
				case SectionType.FileTag:
					if (!ShowFileTagsSection)
						break;
					section = BuildSection("FileTags".GetLocalizedResource(),
						sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
					icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.FileTagsIcon));
					break;
			}

			if (section is not null)
			{
				if (icon is not null)
					section.Icon = icon;

				AddSectionToSideBar(section);

				if (iconIdex is not -1)
					section.Icon = await UIHelpers.GetIconResource(iconIdex);
			}

			return section;
		}

		private static LocationItem BuildSection(string sectionName, SectionType sectionType, ContextMenuOptions options, bool selectsOnInvoked)
			=> new()
			{
				Text = sectionName,
				Section = sectionType,
				MenuOptions = options,
				SelectsOnInvoked = selectsOnInvoked,
				ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
			};

		private void AddSectionToSideBar(LocationItem section)
		{
			var index = sectionOrder
				.TakeWhile(x => x != section.Section)
				.Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0)
				.Sum();
			SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
		}

		private async void UpdateSectionVisibility(SectionType sectionType, bool show)
		{
			if (!show)
			{
				SideBarItems.Remove(SideBarItems.FirstOrDefault(x => x.Section == sectionType));
				return;
			}

			var appearanceSettingsService = userSettingsService.AppearanceSettingsService;

			Func<Task> action = sectionType switch
			{
				SectionType.CloudDrives when appearanceSettingsService.ShowCloudDrivesSection => App.CloudDrivesManager.UpdateDrivesAsync,
				SectionType.Drives => App.DrivesManager.UpdateDrivesAsync,
				SectionType.Network when appearanceSettingsService.ShowNetworkDrivesSection => App.NetworkDrivesManager.UpdateDrivesAsync,
				SectionType.WSL when appearanceSettingsService.ShowWslSection => App.WSLDistroManager.UpdateDrivesAsync,
				SectionType.FileTag when appearanceSettingsService.ShowFileTagsSection => App.FileTagsManager.UpdateFileTagsAsync,
				SectionType.Library => App.LibraryManager.UpdateLibrariesAsync,
				SectionType.Favorites => App.SidebarPinnedController.Model.AddAllItemsToSidebar,
				_ => () => Task.CompletedTask
			};
			Manager_DataChanged(sectionType, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			await action();
		}

		private async void Manager_DataChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await dispatcherQueue.EnqueueAsync(async () =>
			{
				var section = await GetOrCreateSection((SectionType)sender!);
				await SyncSidebarItems(section, getElements, e);
			});

			IReadOnlyList<INavigationControlItem> getElements() => (SectionType)sender switch
			{
				SectionType.Favorites => App.SidebarPinnedController.Model.Favorites,
				SectionType.CloudDrives => App.CloudDrivesManager.Drives,
				SectionType.Drives => App.DrivesManager.Drives,
				SectionType.Network => App.NetworkDrivesManager.Drives,
				SectionType.WSL => App.WSLDistroManager.Distros,
				SectionType.Library => App.LibraryManager.Libraries,
				SectionType.FileTag => App.FileTagsManager.FileTags,
				_ => throw new ArgumentOutOfRangeException(nameof(sender)),
			};
		}

		private void UserSettingsService_OnSettingChangedEvent(object? _, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(IAppearanceSettingsService.IsSidebarOpen):
					OnPropertyChanged(nameof(IsSidebarOpen));
					break;
				case nameof(IAppearanceSettingsService.ShowFavoritesSection):
					UpdateSectionVisibility(SectionType.Favorites, ShowFavoritesSection);
					OnPropertyChanged(nameof(ShowFavoritesSection));
					break;
				case nameof(IAppearanceSettingsService.ShowLibrarySection):
					UpdateSectionVisibility(SectionType.Library, ShowLibrarySection);
					OnPropertyChanged(nameof(ShowLibrarySection));
					break;
				case nameof(IAppearanceSettingsService.ShowCloudDrivesSection):
					UpdateSectionVisibility(SectionType.CloudDrives, ShowCloudDrivesSection);
					OnPropertyChanged(nameof(ShowCloudDrivesSection));
					break;
				case nameof(IAppearanceSettingsService.ShowDrivesSection):
					UpdateSectionVisibility(SectionType.Drives, ShowDrivesSection);
					OnPropertyChanged(nameof(ShowDrivesSection));
					break;
				case nameof(IAppearanceSettingsService.ShowNetworkDrivesSection):
					UpdateSectionVisibility(SectionType.Network, ShowNetworkDrivesSection);
					OnPropertyChanged(nameof(ShowNetworkDrivesSection));
					break;
				case nameof(IAppearanceSettingsService.ShowWslSection):
					UpdateSectionVisibility(SectionType.WSL, ShowWslSection);
					OnPropertyChanged(nameof(ShowWslSection));
					break;
				case nameof(IAppearanceSettingsService.ShowFileTagsSection):
					UpdateSectionVisibility(SectionType.FileTag, ShowFileTagsSection);
					OnPropertyChanged(nameof(ShowFileTagsSection));
					break;
			}
		}
	}
}