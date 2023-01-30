using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using Files.App.Helpers;
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
using static Files.App.Helpers.MenuFlyoutHelper;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class PreferencesViewModel : ObservableObject, IDisposable
	{
		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private bool disposed;
		private ReadOnlyCollection<IMenuFlyoutItemViewModel> addFlyoutItemsSource;

		// Commands

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

		// Lists

		public List<DateTimeFormatItem> DateFormats { get; set; }
		public ObservableCollection<AppLanguageItem> AppLanguages { get; set; }

		public PreferencesViewModel()
		{
			OpenFilesAtStartupCommand = new AsyncRelayCommand(OpenFilesAtStartup);
			ChangePageCommand = new AsyncRelayCommand(ChangePage);
			RemovePageCommand = new RelayCommand(RemovePage);
			AddPageCommand = new RelayCommand<string>(async (path) => await AddPage(path));

			AddSupportedAppLanguages();

			AddDateTimeOptions();
			SelectedDateTimeFormatIndex = (int)Enum.Parse(typeof(DateTimeFormats), DateTimeFormat.ToString());

			dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			if (userSettingsService.PreferencesSettingsService.TabsOnStartupList is not null)
				PagesOnStartupList = new ObservableCollection<PageOnStartupViewModel>(userSettingsService.PreferencesSettingsService.TabsOnStartupList.Select((p) => new PageOnStartupViewModel(p)));
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
			var appLanguages = ApplicationLanguages.ManifestLanguages
				.Append(string.Empty) // add default language id
				.Select(language => new AppLanguageItem(language))
				.OrderBy(language => language.LanguagID is not "") // default language on top
				.ThenBy(language => language.LanguageName);
			AppLanguages = new ObservableCollection<AppLanguageItem>(appLanguages);

			string languageID = ApplicationLanguages.PrimaryLanguageOverride;
			SelectedAppLanguageIndex = AppLanguages
				.IndexOf(AppLanguages.FirstOrDefault(dl => dl.LanguagID == languageID) ?? AppLanguages.First());
		}

		private async Task InitStartupSettingsRecentFoldersFlyout()
		{
			// create Browse and Recent flyout items
			var recentsItem = new MenuFlyoutSubItemViewModel("JumpListRecentGroupHeader".GetLocalizedResource());
			recentsItem.Items.Add(new MenuFlyoutItemViewModel("Home".GetLocalizedResource())
			{
				Command = AddPageCommand,
				CommandParameter = "Home".GetLocalizedResource(),
				Tooltip = "Home".GetLocalizedResource()
			});
			await PopulateRecentItems(recentsItem);

			// ensure recent folders aren't stale since we don't update them with a watcher
			// then update the items source again to actually include those items
			await App.RecentItemsManager.UpdateRecentFoldersAsync();
			await PopulateRecentItems(recentsItem);
		}

		private Task PopulateRecentItems(MenuFlyoutSubItemViewModel menu)
		{
			try
			{
				var recentFolders = App.RecentItemsManager.RecentFolders;
				var currentFolderMenus = menu.Items
					.OfType<MenuFlyoutItemViewModel>()
					.Where(m => m.Text != "Home".GetLocalizedResource())
					.Select(m => m.Text)
					.ToHashSet();

				// add separator if we need one and one wasn't added already
				if (recentFolders.Any() && !currentFolderMenus.Any())
					menu.Items.Add(new MenuFlyoutSeparatorViewModel());

				foreach (var folder in recentFolders)
				{
					if (currentFolderMenus.Contains(folder.Name))
						continue;

					menu.Items.Add(new MenuFlyoutItemViewModel(folder.Name)
					{
						Command = AddPageCommand,
						CommandParameter = folder.RecentPath,
						Tooltip = folder.RecentPath
					});
				}
			}
			catch (Exception ex)
			{
				App.Logger.Info(ex, "Could not fetch recent items");
			}

			// update items source
			AddFlyoutItemsSource = new List<IMenuFlyoutItemViewModel>() {
				new MenuFlyoutItemViewModel("Browse".GetLocalizedResource()) { Command = AddPageCommand },
				menu,
			}.AsReadOnly();

			return Task.CompletedTask;
		}

		private void PagesOnStartupList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (PagesOnStartupList.Count > 0)
				userSettingsService.PreferencesSettingsService.TabsOnStartupList = PagesOnStartupList.Select((p) => p.Path).ToList();
			else
				userSettingsService.PreferencesSettingsService.TabsOnStartupList = null;
		}

		public int SelectedStartupSettingIndex => ContinueLastSessionOnStartUp ? 1 : OpenASpecificPageOnStartup ? 2 : 0;

		public bool OpenNewTabPageOnStartup
		{
			get => userSettingsService.PreferencesSettingsService.OpenNewTabOnStartup;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.OpenNewTabOnStartup)
				{
					userSettingsService.PreferencesSettingsService.OpenNewTabOnStartup = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ContinueLastSessionOnStartUp
		{
			get => userSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp)
				{
					userSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp = value;
					OnPropertyChanged();
				}
			}
		}

		public bool OpenASpecificPageOnStartup
		{
			get => userSettingsService.PreferencesSettingsService.OpenSpecificPageOnStartup;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.OpenSpecificPageOnStartup)
				{
					userSettingsService.PreferencesSettingsService.OpenSpecificPageOnStartup = value;
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
			get => userSettingsService.PreferencesSettingsService.AlwaysOpenNewInstance;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.AlwaysOpenNewInstance)
				{
					userSettingsService.PreferencesSettingsService.AlwaysOpenNewInstance = value;
					ApplicationData.Current.LocalSettings.Values["AlwaysOpenANewInstance"] = value; // Needed in Program.cs
					OnPropertyChanged();
				}
			}
		}

		public bool IsDualPaneEnabled
		{
			get => userSettingsService.PreferencesSettingsService.IsDualPaneEnabled;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.IsDualPaneEnabled)
				{
					userSettingsService.PreferencesSettingsService.IsDualPaneEnabled = value;
					OnPropertyChanged();
				}
			}
		}

		public bool AlwaysOpenDualPaneInNewTab
		{
			get => userSettingsService.PreferencesSettingsService.AlwaysOpenDualPaneInNewTab;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.AlwaysOpenDualPaneInNewTab)
				{
					userSettingsService.PreferencesSettingsService.AlwaysOpenDualPaneInNewTab = value;
					OnPropertyChanged();
				}
			}
		}

		private async Task ChangePage()
		{
			var folderPicker = InitializeWithWindow(new FolderPicker());
			folderPicker.FileTypeFilter.Add("*");
			StorageFolder folder = await folderPicker.PickSingleFolderAsync();

			if (folder is not null)
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
				var folderPicker = InitializeWithWindow(new FolderPicker());
				folderPicker.FileTypeFilter.Add("*");

				var folder = await folderPicker.PickSingleFolderAsync();
				if (folder is not null)
					path = folder.Path;
			}

			if (path is not null && PagesOnStartupList is not null)
				PagesOnStartupList.Add(new PageOnStartupViewModel(path));
		}

		public string DateFormatSample
			=> string.Format("DateFormatSample".GetLocalizedResource(), DateFormats[SelectedDateTimeFormatIndex].Sample1, DateFormats[SelectedDateTimeFormatIndex].Sample2);

		private DispatcherQueue dispatcherQueue;

		public DateTimeFormats DateTimeFormat
		{
			get => userSettingsService.PreferencesSettingsService.DateTimeFormat;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.DateTimeFormat)
				{
					userSettingsService.PreferencesSettingsService.DateTimeFormat = value;
					OnPropertyChanged();
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

		public bool SearchUnindexedItems
		{
			get => userSettingsService.PreferencesSettingsService.SearchUnindexedItems;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.SearchUnindexedItems)
				{
					userSettingsService.PreferencesSettingsService.SearchUnindexedItems = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowFoldersWidget
		{
			get => userSettingsService.PreferencesSettingsService.ShowFoldersWidget;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowFoldersWidget)
					userSettingsService.PreferencesSettingsService.ShowFoldersWidget = value;
			}
		}

		public bool ShowDrivesWidget
		{
			get => userSettingsService.PreferencesSettingsService.ShowDrivesWidget;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowDrivesWidget)
					userSettingsService.PreferencesSettingsService.ShowDrivesWidget = value;
			}
		}

		public bool ShowBundlesWidget
		{
			get => userSettingsService.PreferencesSettingsService.ShowBundlesWidget;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowBundlesWidget)
					userSettingsService.PreferencesSettingsService.ShowBundlesWidget = value;
			}
		}

		public bool ShowRecentFilesWidget
		{
			get => userSettingsService.PreferencesSettingsService.ShowRecentFilesWidget;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowRecentFilesWidget)
					userSettingsService.PreferencesSettingsService.ShowRecentFilesWidget = value;
			}
		}

		public bool ShowFavoritesSection
		{
			get => userSettingsService.PreferencesSettingsService.ShowFavoritesSection;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowFavoritesSection)
				{
					userSettingsService.PreferencesSettingsService.ShowFavoritesSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowLibrarySection
		{
			get => userSettingsService.PreferencesSettingsService.ShowLibrarySection;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowLibrarySection)
				{
					userSettingsService.PreferencesSettingsService.ShowLibrarySection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowDrivesSection
		{
			get => userSettingsService.PreferencesSettingsService.ShowDrivesSection;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowDrivesSection)
				{
					userSettingsService.PreferencesSettingsService.ShowDrivesSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowCloudDrivesSection
		{
			get => userSettingsService.PreferencesSettingsService.ShowCloudDrivesSection;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowCloudDrivesSection)
				{
					userSettingsService.PreferencesSettingsService.ShowCloudDrivesSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowNetworkDrivesSection
		{
			get => userSettingsService.PreferencesSettingsService.ShowNetworkDrivesSection;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowNetworkDrivesSection)
				{
					userSettingsService.PreferencesSettingsService.ShowNetworkDrivesSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowWslSection
		{
			get => userSettingsService.PreferencesSettingsService.ShowWslSection;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowWslSection)
				{
					userSettingsService.PreferencesSettingsService.ShowWslSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowFileTagsSection
		{
			get => userSettingsService.PreferencesSettingsService.ShowFileTagsSection;
			set
			{
				if (value != userSettingsService.PreferencesSettingsService.ShowFileTagsSection)
				{
					userSettingsService.PreferencesSettingsService.ShowFileTagsSection = value;
					OnPropertyChanged();
				}
			}
		}

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				GC.SuppressFinalize(this);
			}
		}

		~PreferencesViewModel()
		{
			Dispose();
		}
	}

	public class PageOnStartupViewModel
	{
		public string Text
		{
			get
			{
				if (Path == "Home".GetLocalizedResource())
					return "Home".GetLocalizedResource();
				return (Path == CommonPaths.RecycleBinPath)
					   ? ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin")
					   : Path;
			}
		}

		public string Path { get; }

		internal PageOnStartupViewModel(string path) => Path = path;
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

