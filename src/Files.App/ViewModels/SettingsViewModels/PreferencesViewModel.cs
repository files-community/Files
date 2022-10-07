using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.Controllers;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Shell;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Files.Shared.Services.DateTimeFormatter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using static Files.App.Helpers.MenuFlyoutHelper;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class PreferencesViewModel : ObservableObject, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private bool disposed;
		private ReadOnlyCollection<IMenuFlyoutItemViewModel> addFlyoutItemsSource;



		// Commands

		public AsyncRelayCommand EditTerminalApplicationsCommand { get; }
		public AsyncRelayCommand OpenFilesAtStartupCommand { get; }
		public AsyncRelayCommand ChangePageCommand { get; }
		public RelayCommand RemovePageCommand { get; }
		public RelayCommand<string> AddPageCommand { get; }



		// Properties

		private bool showRestartControl;
		public bool ShowRestartControl
		{
			get => showRestartControl;
			set => SetProperty(ref showRestartControl, value);
		}

		private int selectedPageIndex = -1;
		public int SelectedPageIndex
		{
			get => selectedPageIndex;
			set
			{
				if (SetProperty(ref selectedPageIndex, value))
					IsPageListEditEnabled = value >= 0;
			}
		}

		private bool isPageListEditEnabled;
		public bool IsPageListEditEnabled
		{
			get => isPageListEditEnabled;
			set => SetProperty(ref isPageListEditEnabled, value);
		}

		private int selectedDateTimeFormatIndex;
		public int SelectedDateTimeFormatIndex
		{
			get => selectedDateTimeFormatIndex;
			set
			{
				if (SetProperty(ref selectedDateTimeFormatIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDateTimeFormatIndex));
					DateTimeFormat = (DateTimeFormats)value;
				}
			}
		}

		private int selectedAppLanguageIndex;
		public int SelectedAppLanguageIndex
		{
			get => selectedAppLanguageIndex;
			set
			{
				if (SetProperty(ref selectedAppLanguageIndex, value))
				{
					OnPropertyChanged(nameof(SelectedAppLanguageIndex));

					if (ApplicationLanguages.PrimaryLanguageOverride != AppLanguages[value].LanguagID)
						ShowRestartControl = true;

					ApplicationLanguages.PrimaryLanguageOverride = AppLanguages[value].LanguagID;
				}
			}
		}

		private Terminal selectedTerminal;
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


		// Lists

		public List<DateTimeFormatItem> DateFormats { get; set; }
		public ObservableCollection<Terminal> Terminals { get; set; }
		public ObservableCollection<AppLanguageItem> AppLanguages { get; set; }

		public PreferencesViewModel()
		{
			EditTerminalApplicationsCommand = new AsyncRelayCommand(LaunchTerminalsConfigFile);
			OpenFilesAtStartupCommand = new AsyncRelayCommand(OpenFilesAtStartup);			
			ChangePageCommand = new AsyncRelayCommand(ChangePage);
			RemovePageCommand = new RelayCommand(RemovePage);
			AddPageCommand = new RelayCommand<string>(async (path) => await AddPage(path));

			AddSupportedAppLanguages();

			Terminals = App.TerminalController.Model.Terminals;
			SelectedTerminal = App.TerminalController.Model.GetDefaultTerminal();

			AddDateTimeOptions();
			SelectedDateTimeFormatIndex = (int)Enum.Parse(typeof(DateTimeFormats), DateTimeFormat.ToString());

			dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			App.TerminalController.ModelChanged += ReloadTerminals;

			if (UserSettingsService.PreferencesSettingsService.TabsOnStartupList != null)
				PagesOnStartupList = new ObservableCollection<PageOnStartupViewModel>(UserSettingsService.PreferencesSettingsService.TabsOnStartupList.Select((p) => new PageOnStartupViewModel(p)));
			else
				PagesOnStartupList = new ObservableCollection<PageOnStartupViewModel>();

			PagesOnStartupList.CollectionChanged += PagesOnStartupList_CollectionChanged;

			_ = InitStartupSettingsRecentFoldersFlyout();
			_ = DetectOpenFilesAtStartup();
		}

		private void AddDateTimeOptions()
		{
			DateTimeOffset sampleDate1 = DateTime.Now;
			DateTimeOffset sampleDate2 = new DateTime(sampleDate1.Year - 5, 12, 31, 14, 30, 0);
			var styles = new DateTimeFormats[] { DateTimeFormats.Application, DateTimeFormats.System, DateTimeFormats.Universal };
			DateFormats = styles.Select(style => new DateTimeFormatItem(style, sampleDate1, sampleDate2)).ToList();
		}

		private void AddSupportedAppLanguages()
		{
			var supportedLanguages = ApplicationLanguages.ManifestLanguages;

			AppLanguages = new ObservableCollection<AppLanguageItem> { };
			foreach (var language in supportedLanguages)
				AppLanguages.Add(new AppLanguageItem(language));

			SelectedAppLanguageIndex = AppLanguages.IndexOf(AppLanguages.FirstOrDefault(dl => dl.LanguagID == ApplicationLanguages.PrimaryLanguageOverride) ?? AppLanguages.FirstOrDefault());
		}

		private async Task InitStartupSettingsRecentFoldersFlyout()
		{
			var recentsItem = new MenuFlyoutSubItemViewModel("JumpListRecentGroupHeader".GetLocalizedResource());
			recentsItem.Items.Add(new MenuFlyoutItemViewModel("Home".GetLocalizedResource())
			{
				Command = AddPageCommand,
				CommandParameter = "Home".GetLocalizedResource(),
				Tooltip = "Home".GetLocalizedResource()
			});

			await App.RecentItemsManager.UpdateRecentFoldersAsync();    // ensure recent folders aren't stale since we don't update them with a watcher
			await PopulateRecentItems(recentsItem).ContinueWith(_ =>
			{
				AddFlyoutItemsSource = new List<IMenuFlyoutItemViewModel>() {
					new MenuFlyoutItemViewModel("Browse".GetLocalizedResource()) { Command = AddPageCommand },
					recentsItem,
				}.AsReadOnly();
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private Task PopulateRecentItems(MenuFlyoutSubItemViewModel menu)
		{
			try
			{
				var recentFolders = App.RecentItemsManager.RecentFolders;

				// add separator
				if (recentFolders.Any())
					menu.Items.Add(new MenuFlyoutSeparatorViewModel());

				foreach (var recentFolder in recentFolders)
				{
					var menuItem = new MenuFlyoutItemViewModel(recentFolder.Name)
					{
						Command = AddPageCommand,
						CommandParameter = recentFolder.RecentPath,
						Tooltip = recentFolder.RecentPath
					};
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
				UserSettingsService.PreferencesSettingsService.TabsOnStartupList = PagesOnStartupList.Select((p) => p.Path).ToList();
			else
				UserSettingsService.PreferencesSettingsService.TabsOnStartupList = null;
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

		public ReadOnlyCollection<IMenuFlyoutItemViewModel> AddFlyoutItemsSource
		{
			get => addFlyoutItemsSource;
			set => SetProperty(ref addFlyoutItemsSource, value);
		}

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
			var folderPicker = this.InitializeWithWindow(new FolderPicker());
			folderPicker.FileTypeFilter.Add("*");
			StorageFolder folder = await folderPicker.PickSingleFolderAsync();

			if (folder != null)
			{
				if (SelectedPageIndex >= 0)
					PagesOnStartupList[SelectedPageIndex] = new PageOnStartupViewModel(folder.Path);
			}
		}

		// WINUI3
		private FolderPicker InitializeWithWindow(FolderPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}

		private void RemovePage()
		{
			int index = SelectedPageIndex;
			if (index >= 0)
			{
				PagesOnStartupList.RemoveAt(index);
				if (index > 0)
					SelectedPageIndex = index - 1;
				else if (PagesOnStartupList.Count > 0)
					SelectedPageIndex = 0;
			}
		}

		private async Task AddPage(string path = null)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				var folderPicker = this.InitializeWithWindow(new FolderPicker());
				folderPicker.FileTypeFilter.Add("*");

				var folder = await folderPicker.PickSingleFolderAsync();
				if (folder != null)
					path = folder.Path;
			}

			if (path != null && PagesOnStartupList != null)
				PagesOnStartupList.Add(new PageOnStartupViewModel(path));
		}

		public class PageOnStartupViewModel
		{
			public string Text
			{
				get
				{
					if (Path == "Home".GetLocalizedResource())
						return "Home".GetLocalizedResource();
					if (Path == CommonPaths.RecycleBinPath)
						return ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
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
			=> string.Format("DateFormatSample".GetLocalizedResource(), DateFormats[SelectedDateTimeFormatIndex].Sample1, DateFormats[SelectedDateTimeFormatIndex].Sample2);


		private DispatcherQueue dispatcherQueue;


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

		public DateTimeFormats DateTimeFormat
		{
			get => UserSettingsService.PreferencesSettingsService.DateTimeFormat;
			set
			{
				if (value != UserSettingsService.PreferencesSettingsService.DateTimeFormat)
				{
					UserSettingsService.PreferencesSettingsService.DateTimeFormat = value;
					OnPropertyChanged();
				}
			}
		}

		private async Task LaunchTerminalsConfigFile()
		{
			var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/terminal.json"));

			if (!await Launcher.LaunchFileAsync(configFile))
				await ContextMenu.InvokeVerb("open", configFile.Path);
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
					await startupTask.RequestEnableAsync();
				else
					startupTask.Disable();
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

		public bool SelectFilesOnHover
		{
			get => UserSettingsService.PreferencesSettingsService.SelectFilesOnHover;
			set
			{
				if (value != UserSettingsService.PreferencesSettingsService.SelectFilesOnHover)
				{
					UserSettingsService.PreferencesSettingsService.SelectFilesOnHover = value;
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

	public class AppLanguageItem
	{
		public string LanguagID { get; set; }
		public string LanguageName { get; set; }

		public AppLanguageItem(string languagID)
		{
			if (!string.IsNullOrEmpty(languagID))
			{
				var info = new CultureInfo(languagID);
				LanguagID = info.Name;
				LanguageName = info.NativeName;
			}
			else
			{
				LanguagID = string.Empty;
				var systemDefaultLanguageOptionStr = "SettingsPreferencesSystemDefaultLanguageOption".GetLocalizedResource();
				LanguageName = string.IsNullOrEmpty(systemDefaultLanguageOptionStr) ? "System Default" : systemDefaultLanguageOptionStr;
			}
		}

		public override string ToString()
		{
			return LanguageName;
		}
	}

	public class DateTimeFormatItem
	{
		public string Label { get; }
		public string Sample1 { get; }
		public string Sample2 { get; }

		public DateTimeFormatItem(DateTimeFormats style, DateTimeOffset sampleDate1, DateTimeOffset sampleDate2)
		{
			var factory = Ioc.Default.GetRequiredService<IDateTimeFormatterFactory>();
			var formatter = factory.GetDateTimeFormatter(style);

			Label = formatter.Name;
			Sample1 = formatter.ToShortLabel(sampleDate1);
			Sample2 = formatter.ToShortLabel(sampleDate2);
		}
	}
}

