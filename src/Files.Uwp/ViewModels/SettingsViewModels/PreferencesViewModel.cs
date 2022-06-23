using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Files.Shared.Services.DateTimeFormatter;
using Files.Uwp.Controllers;
using Files.Uwp.DataModels;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;
using static Files.Uwp.Helpers.MenuFlyoutHelper;

namespace Files.Uwp.ViewModels.SettingsViewModels
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

        public ICommand OpenFilesAtStartupCommand { get; }

        public PreferencesViewModel()
        {
            ChangePageCommand = new AsyncRelayCommand(ChangePage);
            RemovePageCommand = new RelayCommand(RemovePage);
            AddPageCommand = new RelayCommand<string>(async (path) => await AddPage(path));

            DefaultLanguages = App.AppSettings.DefaultLanguages;
            Terminals = App.TerminalController.Model.Terminals;

            DateTimeOffset sampleDate1 = DateTime.Now;
            DateTimeOffset sampleDate2 = new DateTime(sampleDate1.Year - 5, 12, 31, 14, 30, 0);
            var styles = new TimeStyle[] { TimeStyle.Application, TimeStyle.System, TimeStyle.Universal };
            DateFormats = styles.Select(style => new DateFormatItem(style, sampleDate1, sampleDate2)).ToList();

            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            EditTerminalApplicationsCommand = new AsyncRelayCommand(LaunchTerminalsConfigFile);
            OpenFilesAtStartupCommand = new AsyncRelayCommand(OpenFilesAtStartup);
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

            _ = InitStartupSettingsRecentFoldersFlyout();
            _ = DetectOpenFilesAtStartup();
        }

        private async Task InitStartupSettingsRecentFoldersFlyout()
        {
            var recentsItem = new MenuFlyoutSubItemViewModel("JumpListRecentGroupHeader".GetLocalized());
            recentsItem.Items.Add(new MenuFlyoutItemViewModel("Home".GetLocalized(), "Home".GetLocalized(), AddPageCommand));

            await App.RecentItemsManager.UpdateRecentFoldersAsync();    // ensure recent folders aren't stale since we don't update them with a watcher
            await PopulateRecentItems(recentsItem).ContinueWith(_ =>
            {
                AddFlyoutItemsSource = new ReadOnlyCollection<IMenuFlyoutItem>(new IMenuFlyoutItem[] {
                    new MenuFlyoutItemViewModel("Browse".GetLocalized(), null, AddPageCommand),
                    recentsItem,
                });
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private Task PopulateRecentItems(MenuFlyoutSubItemViewModel menu)
        {
            try
            {
                var recentFolders = App.RecentItemsManager.RecentFolders;

                // add separator
                if (recentFolders.Any())
                {
                    menu.Items.Add(new MenuFlyoutSeparatorViewModel());
                }

                foreach (var recentFolder in recentFolders)
                {
                    var menuItem = new MenuFlyoutItemViewModel(recentFolder.Name, recentFolder.RecentPath, AddPageCommand);
                    menu.Items.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                App.Logger.Info(ex, "Could not fetch recent items");
            }

            return Task.CompletedTask;
        }

        private void PagesOnStartupList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PagesOnStartupList.Count > 0)
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

        public ICommand ChangePageCommand { get; }
        public ICommand RemovePageCommand { get; }
        public RelayCommand<string> AddPageCommand { get; }

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

        private async Task ChangePage()
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

        private async Task AddPage(string path = null)
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
                        return "Home".GetLocalized();
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
            dispatcherQueue.EnqueueAsync(() =>
            {
                Terminals = controller.Model.Terminals;
                SelectedTerminal = controller.Model.GetDefaultTerminal();
            });
        }

        public string DateFormatSample
            => string.Format("DateFormatSample".GetLocalized(), DateFormats[SelectedDateFormatIndex].Sample1, DateFormats[SelectedDateFormatIndex].Sample2);

        public List<DateFormatItem> DateFormats { get; set; }

        private DispatcherQueue dispatcherQueue;

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
                    OnPropertyChanged(nameof(DateFormatSample));
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

        private bool openInLogin;

        public bool OpenInLogin
        {
            get => openInLogin;
            set => SetProperty(ref openInLogin, value);
        }

        private bool canOpenInLogin;

        public bool CanOpenInLogin
        {
            get => canOpenInLogin;
            set => SetProperty(ref canOpenInLogin, value);
        }

        public async Task OpenFilesAtStartup()
        {
            var stateMode = await ReadState();

            bool state = stateMode switch
            {
                StartupTaskState.Enabled => true,
                StartupTaskState.EnabledByPolicy => true,
                StartupTaskState.DisabledByPolicy => false,
                StartupTaskState.DisabledByUser => false,
                _ => false,
            };

            if (state != OpenInLogin)
            {
                StartupTask startupTask = await StartupTask.GetAsync("3AA55462-A5FA-4933-88C4-712D0B6CDEBB");
                if (OpenInLogin)
                {
                    await startupTask.RequestEnableAsync();
                }
                else
                {
                    startupTask.Disable();
                }
                await DetectOpenFilesAtStartup();
            }
        }

        public async Task DetectOpenFilesAtStartup()
        {
            var stateMode = await ReadState();

            switch (stateMode)
            {
                case StartupTaskState.Disabled:
                    CanOpenInLogin = true;
                    OpenInLogin = false;
                    break;
                case StartupTaskState.Enabled:
                    CanOpenInLogin = true;
                    OpenInLogin = true;
                    break;
                case StartupTaskState.DisabledByPolicy:
                    CanOpenInLogin = false;
                    OpenInLogin = false;
                    break;
                case StartupTaskState.DisabledByUser:
                    CanOpenInLogin = false;
                    OpenInLogin = false;
                    break;
                case StartupTaskState.EnabledByPolicy:
                    CanOpenInLogin = false;
                    OpenInLogin = true;
                    break;
            }
        }

        public async Task<StartupTaskState> ReadState()
        {
            var state = await StartupTask.GetAsync("3AA55462-A5FA-4933-88C4-712D0B6CDEBB");
            return state.State;
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

        public bool AreAlternateStreamsVisible
        {
            get => UserSettingsService.PreferencesSettingsService.AreAlternateStreamsVisible;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.AreAlternateStreamsVisible)
                {
                    UserSettingsService.PreferencesSettingsService.AreAlternateStreamsVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowDotFiles
        {
            get => UserSettingsService.PreferencesSettingsService.ShowDotFiles;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.ShowDotFiles)
                {
                    UserSettingsService.PreferencesSettingsService.ShowDotFiles = value;
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

        public bool ShowThumbnails
        {
            get => UserSettingsService.PreferencesSettingsService.ShowThumbnails;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.ShowThumbnails)
                {
                    UserSettingsService.PreferencesSettingsService.ShowThumbnails = value;
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
            get => UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles;
            set
            {
                if (value != UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles)
                {
                    UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = value;
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

    public class DateFormatItem
    {
        public string Label { get; }
        public string Sample1 { get; }
        public string Sample2 { get; }

        public DateFormatItem(TimeStyle style, DateTimeOffset sampleDate1, DateTimeOffset sampleDate2)
        {
            var factory = Ioc.Default.GetService<IDateTimeFormatterFactory>();
            var formatter = factory.GetDateTimeFormatter(style);

            Label = formatter.Name;
            Sample1 = formatter.ToShortLabel(sampleDate1);
            Sample2 = formatter.ToShortLabel(sampleDate2);
        }
    }
}
