using Files.Common;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using static Files.Helpers.MenuFlyoutHelper;

namespace Files.ViewModels.SettingsViewModels
{
    public class OnStartupViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private int selectedPageIndex = -1;
        private bool isPageListEditEnabled;
        private ReadOnlyCollection<IMenuFlyoutItem> addFlyoutItemsSource;

        public OnStartupViewModel()
        {
            if (UserSettingsService.StartupSettingsService.TabsOnStartupList != null)
            {
                PagesOnStartupList = new ObservableCollection<PageOnStartupViewModel>(UserSettingsService.StartupSettingsService.TabsOnStartupList.Select((p) => new PageOnStartupViewModel(p)));
            }
            else
            {
                PagesOnStartupList = new ObservableCollection<PageOnStartupViewModel>();
            }

            PagesOnStartupList.CollectionChanged += PagesOnStartupList_CollectionChanged;

            var recentsItem = new MenuFlyoutSubItemViewModel("JumpListRecentGroupHeader".GetLocalized());
            recentsItem.Items.Add(new MenuFlyoutItemViewModel("SidebarHome".GetLocalized(), "Home".GetLocalized(), AddPageCommand));
            PopulateRecentItems(recentsItem).ContinueWith(_ =>
            {
                AddFlyoutItemsSource = new ReadOnlyCollection<IMenuFlyoutItem>(new IMenuFlyoutItem[] {
                    new MenuFlyoutItemViewModel("Browse".GetLocalized(), null, AddPageCommand),
                    recentsItem,
                });
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task PopulateRecentItems(MenuFlyoutSubItemViewModel menu)
        {
            bool hasRecents = false;
            menu.Items.Add(new MenuFlyoutSeparatorViewModel());

            try
            {
                var mostRecentlyUsed = StorageApplicationPermissions.MostRecentlyUsedList;

                foreach (AccessListEntry entry in mostRecentlyUsed.Entries)
                {
                    string mruToken = entry.Token;
                    var added = await FilesystemTasks.Wrap(async () =>
                    {
                        IStorageItem item = await mostRecentlyUsed.GetItemAsync(mruToken, AccessCacheOptions.FastLocationsOnly);
                        if (item.IsOfType(StorageItemTypes.Folder))
                        {
                            menu.Items.Add(new MenuFlyoutItemViewModel(item.Name, string.IsNullOrEmpty(item.Path) ? entry.Metadata : item.Path, AddPageCommand));
                            hasRecents = true;
                        }
                    });
                    if (added == FileSystemStatusCode.Unauthorized)
                    {
                        // Skip item until consent is provided
                    }
                    // Exceptions include but are not limited to:
                    // COMException, FileNotFoundException, ArgumentException, DirectoryNotFoundException
                    // 0x8007016A -> The cloud file provider is not running
                    // 0x8000000A -> The data necessary to complete this operation is not yet available
                    // 0x80004005 -> Unspecified error
                    // 0x80270301 -> ?
                    else if (!added)
                    {
                        await FilesystemTasks.Wrap(() =>
                        {
                            mostRecentlyUsed.Remove(mruToken);
                            return Task.CompletedTask;
                        });
                        System.Diagnostics.Debug.WriteLine(added.ErrorCode);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Info(ex, "Could not fetch recent items");
            }

            if (!hasRecents)
            {
                menu.Items.RemoveAt(menu.Items.Count - 1);
            }
        }

        private void PagesOnStartupList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PagesOnStartupList.Count() > 0)
            {
                UserSettingsService.StartupSettingsService.TabsOnStartupList = PagesOnStartupList.Select((p) => p.Path).ToList();
            }
            else
            {
                UserSettingsService.StartupSettingsService.TabsOnStartupList = null;
            }
        }

        public bool OpenNewTabPageOnStartup
        {
            get => UserSettingsService.StartupSettingsService.OpenNewTabOnStartup;
            set
            {
                if (value != UserSettingsService.StartupSettingsService.OpenNewTabOnStartup)
                {
                    UserSettingsService.StartupSettingsService.OpenNewTabOnStartup = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ContinueLastSessionOnStartUp
        {
            get => UserSettingsService.StartupSettingsService.ContinueLastSessionOnStartUp;
            set
            {
                if (value != UserSettingsService.StartupSettingsService.ContinueLastSessionOnStartUp)
                {
                    UserSettingsService.StartupSettingsService.ContinueLastSessionOnStartUp = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OpenASpecificPageOnStartup
        {
            get => UserSettingsService.StartupSettingsService.OpenSpecificPageOnStartup;
            set
            {
                if (value != UserSettingsService.StartupSettingsService.OpenSpecificPageOnStartup)
                {
                    UserSettingsService.StartupSettingsService.OpenSpecificPageOnStartup = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<PageOnStartupViewModel> PagesOnStartupList { get; set; }

        public int SelectedPageIndex
        {
            get => selectedPageIndex;
            set
            {
                if (SetProperty(ref selectedPageIndex, value))
                {
                    IsPageListEditEnabled = value >= 0;
                }
            }
        }

        public bool IsPageListEditEnabled
        {
            get => isPageListEditEnabled;
            set => SetProperty(ref isPageListEditEnabled, value);
        }

        public ReadOnlyCollection<IMenuFlyoutItem> AddFlyoutItemsSource
        {
            get => addFlyoutItemsSource;
            set => SetProperty(ref addFlyoutItemsSource, value);
        }

        public RelayCommand ChangePageCommand => new RelayCommand(ChangePage);
        public RelayCommand RemovePageCommand => new RelayCommand(RemovePage);
        public RelayCommand<string> AddPageCommand => new RelayCommand<string>(AddPage);

        public bool AlwaysOpenANewInstance
        {
            get => UserSettingsService.StartupSettingsService.AlwaysOpenNewInstance;
            set
            {
                if (value != UserSettingsService.StartupSettingsService.AlwaysOpenNewInstance)
                {
                    UserSettingsService.StartupSettingsService.AlwaysOpenNewInstance = value;
                    ApplicationData.Current.LocalSettings.Values["AlwaysOpenANewInstance"] = value; // Needed in Program.cs
                    OnPropertyChanged();
                }
            }
        }

        private async void ChangePage()
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                if (SelectedPageIndex >= 0)
                {
                    PagesOnStartupList[SelectedPageIndex] = new PageOnStartupViewModel(folder.Path);
                }
            }
        }

        private void RemovePage()
        {
            int index = SelectedPageIndex;
            if (index >= 0)
            {
                PagesOnStartupList.RemoveAt(index);
                if (index > 0)
                {
                    SelectedPageIndex = index - 1;
                }
                else if (PagesOnStartupList.Count > 0)
                {
                    SelectedPageIndex = 0;
                }
            }
        }

        private async void AddPage(string path = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    path = folder.Path;
                }
            }

            if (path != null && PagesOnStartupList != null)
            {
                PagesOnStartupList.Add(new PageOnStartupViewModel(path));
            }
        }

        public class PageOnStartupViewModel
        {
            public string Text
            {
                get
                {
                    if (Path == "Home".GetLocalized())
                    {
                        return "SidebarHome".GetLocalized();
                    }
                    if (Path == CommonPaths.RecycleBinPath)
                    {
                        return ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                    }
                    return Path;
                }
            }

            public string Path { get; }

            internal PageOnStartupViewModel(string path) => Path = path;
        }
    }
}