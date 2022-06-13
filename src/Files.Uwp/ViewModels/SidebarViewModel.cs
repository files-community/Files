using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Files.Shared.Extensions;
using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Files.Uwp.UserControls;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.ViewModels
{
    public class SidebarViewModel : ObservableObject, IDisposable
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();
        public ICommand EmptyRecycleBinCommand { get; private set; }

        private IPaneHolder paneHolder;

        public IPaneHolder PaneHolder
        {
            get => paneHolder;
            set => SetProperty(ref paneHolder, value);
        }

        public IFilesystemHelpers FilesystemHelpers => PaneHolder?.FilesystemHelpers;

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

        public bool IsSidebarCompactSize => SidebarDisplayMode == NavigationViewDisplayMode.Compact || SidebarDisplayMode == NavigationViewDisplayMode.Minimal;

        public void NotifyInstanceRelatedPropertiesChanged(string arg)
        {
            UpdateSidebarSelectedItemFromArgs(arg);

            OnPropertyChanged(nameof(SidebarSelectedItem));
        }

        public void UpdateSidebarSelectedItemFromArgs(string arg)
        {
            var value = arg;

            INavigationControlItem item = null;
            List<INavigationControlItem> sidebarItems = SideBarItems
                .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                .Concat(SideBarItems.Where(x => (x as LocationItem)?.ChildItems != null).SelectMany(x => (x as LocationItem).ChildItems).Where(x => !string.IsNullOrWhiteSpace(x.Path)))
                .ToList();

            if (string.IsNullOrEmpty(value))
            {
                //SidebarSelectedItem = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home".GetLocalized()));
                return;
            }

            item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value + "\\", StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                if (value == "Home".GetLocalized())
                {
                    item = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home".GetLocalized()));
                }
            }

            if (SidebarSelectedItem != item)
            {
                SidebarSelectedItem = item;
            }
        }

        public bool IsSidebarOpen
        {
            get => UserSettingsService.AppearanceSettingsService.IsSidebarOpen;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.IsSidebarOpen)
                {
                    UserSettingsService.AppearanceSettingsService.IsSidebarOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowFavoritesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowFavoritesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowFavoritesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowFavoritesSection = value;
                }
            }
        }

        public bool ShowLibrarySection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowLibrarySection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowLibrarySection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowLibrarySection = value;
                }
            }
        }

        public bool ShowDrivesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowDrivesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowDrivesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowDrivesSection = value;
                }
            }
        }

        public bool ShowCloudDrivesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection = value;
                }
            }
        }

        public bool ShowNetworkDrivesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection = value;
                }
            }
        }

        public bool ShowWslSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowWslSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowWslSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowWslSection = value;
                }
            }
        }

        public bool ShowFileTagsSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowFileTagsSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowFileTagsSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowFileTagsSection = value;
                }
            }
        }

        public bool AreFileTagsEnabled
        {
            get => UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled;
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
            EmptyRecycleBinCommand = new RelayCommand<RoutedEventArgs>(EmptyRecycleBin);
            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
            CreateItemHome();

            Manager_DataChanged(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            Manager_DataChanged(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            Manager_DataChanged(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            Manager_DataChanged(SectionType.CloudDrives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            Manager_DataChanged(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            Manager_DataChanged(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            Manager_DataChanged(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            App.SidebarPinnedController.DataChanged += Manager_DataChanged;
            App.LibraryManager.DataChanged += Manager_DataChanged;
            App.DrivesManager.DataChanged += Manager_DataChanged;
            App.CloudDrivesManager.DataChanged += Manager_DataChanged;
            App.NetworkDrivesManager.DataChanged += Manager_DataChanged;
            App.WSLDistroManager.DataChanged += Manager_DataChanged;
            App.FileTagsManager.DataChanged += Manager_DataChanged;
        }

        private async void CreateItemHome()
        {
            var home = await GetOrCreateSection(SectionType.Home);
            SideBarItems.Add(home);
        }

        private async void Manager_DataChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await dispatcherQueue.EnqueueAsync(async () =>
            {
                var section = await GetOrCreateSection((SectionType)sender);
                Func<IReadOnlyList<INavigationControlItem>> getElements = () => (SectionType)sender switch
                {
                    SectionType.Favorites => App.SidebarPinnedController.Model.Favorites,
                    SectionType.CloudDrives => App.CloudDrivesManager.Drives,
                    SectionType.Drives => App.DrivesManager.Drives,
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
            if (section == null)
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

        private bool IsLibraryOnSidebar(LibraryLocationItem item) => item != null && !item.IsEmpty && item.IsDefaultLocation;

        private async Task AddElementToSection(INavigationControlItem elem, LocationItem section, int index = -1)
        {
            if (elem is LibraryLocationItem lib)
            {
                if (IsLibraryOnSidebar(lib) && await lib.CheckDefaultSaveFolderAccess())
                {
                    if (!section.ChildItems.Any(x => x.Path == lib.Path))
                    {
                        lib.Font = App.MainViewModel.FontName;
                        section.ChildItems.AddSorted(elem);
                        await lib.LoadLibraryIcon();
                    }
                }
            }
            else if (elem is DriveItem drive)
            {
                if (!section.ChildItems.Any(x => x.Path == drive.Path))
                {
                    section.ChildItems.Insert(index < 0 ? section.ChildItems.Count : Math.Min(index, section.ChildItems.Count), drive);
                    await drive.LoadDriveIcon();
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
                section.IsExpanded = App.AppSettings.Get(section.Text == "SidebarFavorites".GetLocalized(), $"section:{section.Text.Replace('\\', '_')}");
            }
        }

        private async Task<LocationItem> GetOrCreateSection(SectionType sectionType)
        {
            var sectionOrder = new[] { SectionType.Home, SectionType.Favorites, SectionType.Library,
                SectionType.Drives, SectionType.CloudDrives, SectionType.Network, SectionType.WSL, SectionType.FileTag };
            switch (sectionType)
            {
                case SectionType.Home:
                    return new LocationItem()
                    {
                        Text = "Home".GetLocalized(),
                        Section = SectionType.Home,
                        MenuOptions = new ContextMenuOptions
                        {
                            IsLocationItem = true
                        },
                        Font = App.MainViewModel.FontName,
                        IsDefaultLocation = true,
                        Icon = new BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/Home.png")),
                        Path = "Home".GetLocalized()
                    };
                case SectionType.Favorites:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "SidebarFavorites".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowFavoritesSection && section == null)
                        {
                            section = new LocationItem()
                            {
                                Text = "SidebarFavorites".GetLocalized(),
                                Section = SectionType.Favorites,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                Font = App.MainViewModel.FontName,
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                            section.Icon = new BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/Favorites.png")); // After insert
                        }
                        return section;
                    }

                case SectionType.Library:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "SidebarLibraries".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowLibrarySection && section == null)
                        {
                            section = new LocationItem
                            {
                                Text = "SidebarLibraries".GetLocalized(),
                                Section = SectionType.Library,
                                MenuOptions = new ContextMenuOptions
                                {
                                    IsLibrariesHeader = true,
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                            section.Icon = await UIHelpers.GetIconResource(Constants.ImageRes.Libraries); // After insert
                        }
                        return section;
                    }

                case SectionType.Drives:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "Drives".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowDrivesSection && section == null)
                        {
                            section = new LocationItem()
                            {
                                Text = "Drives".GetLocalized(),
                                Section = SectionType.Drives,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                            section.Icon = await UIHelpers.GetIconResource(Constants.ImageRes.ThisPC); // After insert
                        }
                        return section;
                    }

                case SectionType.CloudDrives:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "SidebarCloudDrives".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection && section == null && App.CloudDrivesManager.Drives.Any())
                        {
                            section = new LocationItem()
                            {
                                Text = "SidebarCloudDrives".GetLocalized(),
                                Section = SectionType.CloudDrives,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                Icon = new BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/CloudDrive.png")),
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                        }
                        return section;
                    }

                case SectionType.Network:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "SidebarNetworkDrives".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection && section == null)
                        {
                            section = new LocationItem()
                            {
                                Text = "SidebarNetworkDrives".GetLocalized(),
                                Section = SectionType.Network,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                            section.Icon = await UIHelpers.GetIconResource(Constants.ImageRes.NetworkDrives); // After insert
                        }
                        return section;
                    }

                case SectionType.WSL:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "WSL".GetLocalized()) as LocationItem;
                        if (UserSettingsService.AppearanceSettingsService.ShowWslSection && section == null && App.WSLDistroManager.Distros.Any())
                        {
                            section = new LocationItem()
                            {
                                Text = "WSL".GetLocalized(),
                                Section = SectionType.WSL,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                Icon = new BitmapImage(new Uri("ms-appx:///Assets/WSL/genericpng.png")),
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                        }
                        return section;
                    }

                case SectionType.FileTag:
                    {
                        var section = SideBarItems.FirstOrDefault(x => x.Text == "FileTags".GetLocalized()) as LocationItem;
                        if (UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled && UserSettingsService.AppearanceSettingsService.ShowFileTagsSection && section == null)
                        {
                            section = new LocationItem()
                            {
                                Text = "FileTags".GetLocalized(),
                                Section = SectionType.FileTag,
                                MenuOptions = new ContextMenuOptions
                                {
                                    ShowHideSection = true
                                },
                                SelectsOnInvoked = false,
                                Icon = new BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/FileTags.png")),
                                ChildItems = new BulkConcurrentObservableCollection<INavigationControlItem>()
                            };
                            var index = sectionOrder.TakeWhile(x => x != sectionType).Select(x => SideBarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
                            SideBarItems.Insert(Math.Min(index, SideBarItems.Count), section);
                        }
                        return section;
                    }
                default:
                    return null;
            }
        }

        public async void UpdateSectionVisibility(SectionType sectionType, bool show)
        {
            if (show)
            {
                var appearanceSettingsService = UserSettingsService.AppearanceSettingsService;

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
            else
            {
                SideBarItems.Remove(SideBarItems.FirstOrDefault(x => x.Section == sectionType));
            }
        }

        public async void EmptyRecycleBin(RoutedEventArgs e)
        {
            await RecycleBinHelpers.S_EmptyRecycleBin();
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
                case nameof(UserSettingsService.AppearanceSettingsService.ShowFavoritesSection):
                    UpdateSectionVisibility(SectionType.Favorites, ShowFavoritesSection);
                    OnPropertyChanged(nameof(ShowFavoritesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowLibrarySection):
                    UpdateSectionVisibility(SectionType.Library, ShowLibrarySection);
                    OnPropertyChanged(nameof(ShowLibrarySection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection):
                    UpdateSectionVisibility(SectionType.CloudDrives, ShowCloudDrivesSection);
                    OnPropertyChanged(nameof(ShowCloudDrivesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowDrivesSection):
                    UpdateSectionVisibility(SectionType.Drives, ShowDrivesSection);
                    OnPropertyChanged(nameof(ShowDrivesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection):
                    UpdateSectionVisibility(SectionType.Network, ShowNetworkDrivesSection);
                    OnPropertyChanged(nameof(ShowNetworkDrivesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowWslSection):
                    UpdateSectionVisibility(SectionType.WSL, ShowWslSection);
                    OnPropertyChanged(nameof(ShowWslSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowFileTagsSection):
                    UpdateSectionVisibility(SectionType.FileTag, ShowFileTagsSection);
                    OnPropertyChanged(nameof(ShowFileTagsSection));
                    break;
                case nameof(UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled):
                    OnPropertyChanged(nameof(AreFileTagsEnabled));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.UseCompactStyles):
                    new SettingsViewModels.AppearanceViewModel().SetCompactStyles(true);
                    break;
            }
        }

        public void Dispose()
        {
            UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;

            App.SidebarPinnedController.DataChanged -= Manager_DataChanged;
            App.LibraryManager.DataChanged -= Manager_DataChanged;
            App.DrivesManager.DataChanged -= Manager_DataChanged;
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