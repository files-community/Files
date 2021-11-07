using Files.Common;
using Files.Controllers;
using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;
using static Files.Helpers.MenuFlyoutHelper;

namespace Files.ViewModels.SettingsViewModels
{
    public class PreferencesViewModel : ObservableObject, IDisposable
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private int selectedLanguageIndex = App.AppSettings.DefaultLanguages.IndexOf(App.AppSettings.DefaultLanguage);
        private Terminal selectedTerminal = App.TerminalController.Model.GetDefaultTerminal();
        private int selectedDateFormatIndex = (int)Enum.Parse(typeof(TimeStyle), App.AppSettings.DisplayedTimeStyle.ToString());

        private bool showRestartControl;
        private List<Terminal> terminals;
        private bool disposed;
        private int selectedPageIndex = -1;
        private bool isPageListEditEnabled;
        private ReadOnlyCollection<IMenuFlyoutItem> addFlyoutItemsSource;

        public ICommand EditTerminalApplicationsCommand { get; }

        public PreferencesViewModel()
        {
            DefaultLanguages = App.AppSettings.DefaultLanguages;
            Terminals = App.TerminalController.Model.Terminals;
            DateFormats = new List<string>
            {
                "ApplicationTimeStye".GetLocalized(),
                "SystemTimeStye".GetLocalized()
            };

            EditTerminalApplicationsCommand = new AsyncRelayCommand(LaunchTerminalsConfigFile);
            App.TerminalController.ModelChanged += ReloadTerminals;

            if (UserSettingsService.PreferencesSettingsService.TabsOnStartupList != null)
            {
                PagesOnStartupList = new ObservableCollection<PageOnStartupViewModel>(UserSettingsService.PreferencesSettingsService.TabsOnStartupList.Select((p) => new PageOnStartupViewModel(p)));
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
                UserSettingsService.PreferencesSettingsService.TabsOnStartupList = PagesOnStartupList.Select((p) => p.Path).ToList();
            }
            else
            {
                UserSettingsService.PreferencesSettingsService.TabsOnStartupList = null;
            }
        }

        public int SelectedStartupSettingIndex => ContinueLastSessionOnStartUp ? 1 : OpenASpecificPageOnStartup ? 2 : 0;

        public bool OpenNewTabPageOnStartup
        {
            get => UserSettingsService.PreferencesSettingsService.OpenNewTabOnStartup;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.OpenNewTabOnStartup)
                {
                    UserSettingsService.PreferencesSettingsService.OpenNewTabOnStartup = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ContinueLastSessionOnStartUp
        {
            get => UserSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp)
                {
                    UserSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OpenASpecificPageOnStartup
        {
            get => UserSettingsService.PreferencesSettingsService.OpenSpecificPageOnStartup;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.OpenSpecificPageOnStartup)
                {
                    UserSettingsService.PreferencesSettingsService.OpenSpecificPageOnStartup = value;
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
            get => UserSettingsService.PreferencesSettingsService.AlwaysOpenNewInstance;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.AlwaysOpenNewInstance)
                {
                    UserSettingsService.PreferencesSettingsService.AlwaysOpenNewInstance = value;
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

        private void ReloadTerminals(TerminalController controller)
        {
            Terminals = controller.Model.Terminals;
            SelectedTerminal = controller.Model.GetDefaultTerminal();
        }

        public List<string> DateFormats { get; set; }

        public int SelectedDateFormatIndex
        {
            get
            {
                return selectedDateFormatIndex;
            }
            set
            {
                if (SetProperty(ref selectedDateFormatIndex, value))
                {
                    App.AppSettings.DisplayedTimeStyle = (TimeStyle)value;
                }
            }
        }

        public ObservableCollection<DefaultLanguageModel> DefaultLanguages { get; set; }

        public int SelectedLanguageIndex
        {
            get { return selectedLanguageIndex; }
            set
            {
                if (SetProperty(ref selectedLanguageIndex, value))
                {
                    App.AppSettings.DefaultLanguage = DefaultLanguages[value];

                    if (App.AppSettings.CurrentLanguage.ID != DefaultLanguages[value].ID)
                    {
                        ShowRestartControl = true;
                    }
                    else
                    {
                        ShowRestartControl = false;
                    }
                }
            }
        }

        public bool ShowRestartControl
        {
            get => showRestartControl;
            set => SetProperty(ref showRestartControl, value);
        }

        public List<Terminal> Terminals
        {
            get => terminals;
            set => SetProperty(ref terminals, value);
        }

        public Terminal SelectedTerminal
        {
            get { return selectedTerminal; }
            set
            {
                if (value is not null && SetProperty(ref selectedTerminal, value))
                {
                    App.TerminalController.Model.DefaultTerminalName = value.Name;
                    App.TerminalController.SaveModel();
                }
            }
        }

        public bool ShowConfirmDeleteDialog
        {
            get => UserSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog)
                {
                    UserSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OpenFoldersNewTab
        {
            get => UserSettingsService.PreferencesSettingsService.OpenFoldersInNewTab;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                {
                    UserSettingsService.PreferencesSettingsService.OpenFoldersInNewTab = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task LaunchTerminalsConfigFile()
        {
            var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/terminal.json"));

            if (!await Launcher.LaunchFileAsync(configFile))
            {
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    await connection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "InvokeVerb" },
                        { "FilePath", configFile.Path },
                        { "Verb", "open" }
                    });
                }
            }
        }

        public bool AreHiddenItemsVisible
        {
            get => UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible)
                {
                    UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AreSystemItemsHidden
        {
            get => UserSettingsService.PreferencesSettingsService.AreSystemItemsHidden;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.AreSystemItemsHidden)
                {
                    UserSettingsService.PreferencesSettingsService.AreSystemItemsHidden = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowFileExtensions
        {
            get => UserSettingsService.PreferencesSettingsService.ShowFileExtensions;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.ShowFileExtensions)
                {
                    UserSettingsService.PreferencesSettingsService.ShowFileExtensions = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OpenFilesWithOneClick
        {
            get => UserSettingsService.PreferencesSettingsService.OpenFilesWithOneClick;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.OpenFilesWithOneClick)
                {
                    UserSettingsService.PreferencesSettingsService.OpenFilesWithOneClick = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OpenFoldersWithOneClick
        {
            get => UserSettingsService.PreferencesSettingsService.OpenFoldersWithOneClick;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.OpenFoldersWithOneClick)
                {
                    UserSettingsService.PreferencesSettingsService.OpenFoldersWithOneClick = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ListAndSortDirectoriesAlongsideFiles
        {
            get => UserSettingsService.PreferencesSettingsService.ListAndSortDirectoriesAlongsideFiles;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.ListAndSortDirectoriesAlongsideFiles)
                {
                    UserSettingsService.PreferencesSettingsService.ListAndSortDirectoriesAlongsideFiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SearchUnindexedItems
        {
            get => UserSettingsService.PreferencesSettingsService.SearchUnindexedItems;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.SearchUnindexedItems)
                {
                    UserSettingsService.PreferencesSettingsService.SearchUnindexedItems = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AreLayoutPreferencesPerFolder
        {
            get => UserSettingsService.PreferencesSettingsService.AreLayoutPreferencesPerFolder;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.AreLayoutPreferencesPerFolder)
                {
                    UserSettingsService.PreferencesSettingsService.AreLayoutPreferencesPerFolder = value;
                    OnPropertyChanged();
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                App.TerminalController.ModelChanged -= ReloadTerminals;
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~PreferencesViewModel()
        {
            Dispose();
        }
    }
}