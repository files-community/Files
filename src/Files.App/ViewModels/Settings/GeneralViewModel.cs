// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.Storage;
using Windows.Storage.Pickers;
using static Files.App.Helpers.MenuFlyoutHelper;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Files.App.ViewModels.Settings
{
	public class GeneralViewModel : ObservableObject, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; }

		private bool disposed;

		private ReadOnlyCollection<IMenuFlyoutItemViewModel> addFlyoutItemsSource;

		public AsyncRelayCommand OpenFilesAtStartupCommand { get; }
		public AsyncRelayCommand ChangePageCommand { get; }
		public RelayCommand RemovePageCommand { get; }
		public RelayCommand<string> AddPageCommand { get; }

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

		public List<DateTimeFormatItem> DateFormats { get; set; }

		public ObservableCollection<AppLanguageItem> AppLanguages { get; set; }

		public int SelectedStartupSettingIndex
			=> ContinueLastSessionOnStartUp ? 1 : OpenASpecificPageOnStartup ? 2 : 0;

		public bool OpenNewTabPageOnStartup
		{
			get => UserSettingsService.GeneralSettingsService.OpenNewTabOnStartup;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.OpenNewTabOnStartup)
				{
					UserSettingsService.GeneralSettingsService.OpenNewTabOnStartup = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ContinueLastSessionOnStartUp
		{
			get => UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp)
				{
					UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp = value;

					OnPropertyChanged();
				}
			}
		}

		public bool OpenASpecificPageOnStartup
		{
			get => UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup)
				{
					UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup = value;

					OnPropertyChanged();
				}
			}
		}

		public ObservableCollection<PageOnStartupItem> PagesOnStartupList { get; set; }

		public ReadOnlyCollection<IMenuFlyoutItemViewModel> AddFlyoutItemsSource
		{
			get => addFlyoutItemsSource;
			set => SetProperty(ref addFlyoutItemsSource, value);
		}

		public bool AlwaysOpenANewInstance
		{
			get => UserSettingsService.GeneralSettingsService.AlwaysOpenNewInstance;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.AlwaysOpenNewInstance)
				{
					UserSettingsService.GeneralSettingsService.AlwaysOpenNewInstance = value;

					// Needed in Program.cs
					ApplicationData.Current.LocalSettings.Values["AlwaysOpenANewInstance"] = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowOpenInNewPane
		{
			get => UserSettingsService.GeneralSettingsService.ShowOpenInNewPane;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowOpenInNewPane)
				{
					UserSettingsService.GeneralSettingsService.ShowOpenInNewPane = value;

					OnPropertyChanged();
				}
			}
		}

		public bool AlwaysOpenDualPaneInNewTab
		{
			get => UserSettingsService.GeneralSettingsService.AlwaysOpenDualPaneInNewTab;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.AlwaysOpenDualPaneInNewTab)
				{
					UserSettingsService.GeneralSettingsService.AlwaysOpenDualPaneInNewTab = value;

					OnPropertyChanged();
				}
			}
		}

		public string DateFormatSample
			=> string.Format("DateFormatSample".GetLocalizedResource(), DateFormats[SelectedDateTimeFormatIndex].Sample1, DateFormats[SelectedDateTimeFormatIndex].Sample2);

		private DispatcherQueue dispatcherQueue;

		public DateTimeFormats DateTimeFormat
		{
			get => UserSettingsService.GeneralSettingsService.DateTimeFormat;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.DateTimeFormat)
				{
					UserSettingsService.GeneralSettingsService.DateTimeFormat = value;
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

		public bool SearchUnindexedItems
		{
			get => UserSettingsService.GeneralSettingsService.SearchUnindexedItems;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.SearchUnindexedItems)
				{
					UserSettingsService.GeneralSettingsService.SearchUnindexedItems = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowQuickAccessWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget)
					UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget = value;
			}
		}

		public bool ShowDrivesWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowDrivesWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowDrivesWidget)
					UserSettingsService.GeneralSettingsService.ShowDrivesWidget = value;
			}
		}

		public bool ShowBundlesWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowBundlesWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowBundlesWidget)
					UserSettingsService.GeneralSettingsService.ShowBundlesWidget = value;

				if (value & ShowFileTagsWidget)
					ShowFileTagsWidget = false;

				OnPropertyChanged();
			}
		}
		public bool ShowFileTagsWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowFileTagsWidget)
					UserSettingsService.GeneralSettingsService.ShowFileTagsWidget = value;

				if (value & ShowBundlesWidget)
					ShowBundlesWidget = false;

				OnPropertyChanged();
			}
		}

		public bool ShowRecentFilesWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget)
					UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget = value;
			}
		}

		public bool MoveShellExtensionsToSubMenu
		{
			get => UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
				{
					UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowEditTagsMenu
		{
			get => UserSettingsService.GeneralSettingsService.ShowEditTagsMenu;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowEditTagsMenu)
				{
					UserSettingsService.GeneralSettingsService.ShowEditTagsMenu = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowOpenInNewTab
		{
			get => UserSettingsService.GeneralSettingsService.ShowOpenInNewTab;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowOpenInNewTab)
				{
					UserSettingsService.GeneralSettingsService.ShowOpenInNewTab = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowOpenInNewWindow
		{
			get => UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow)
				{
					UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow = value;
					OnPropertyChanged();
				}
			}
		}

		public GeneralViewModel()
		{
			UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			AddSupportedAppLanguages();

			AddDateTimeOptions();
			SelectedDateTimeFormatIndex = (int)Enum.Parse(typeof(DateTimeFormats), DateTimeFormat.ToString());

			dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			if (UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
				PagesOnStartupList = new ObservableCollection<PageOnStartupItem>(UserSettingsService.GeneralSettingsService.TabsOnStartupList.Select((p) => new PageOnStartupItem(p)));
			else
				PagesOnStartupList = new ObservableCollection<PageOnStartupItem>();

			OpenFilesAtStartupCommand = new AsyncRelayCommand(OpenFilesAtStartup);
			ChangePageCommand = new AsyncRelayCommand(ChangePage);
			RemovePageCommand = new RelayCommand(RemovePage);
			AddPageCommand = new RelayCommand<string>(async (path) => await AddPage(path));

			PagesOnStartupList.CollectionChanged += PagesOnStartupList_CollectionChanged;

			_ = InitStartupSettingsRecentFoldersFlyout();
			_ = DetectOpenFilesAtStartup();
		}

		private void AddDateTimeOptions()
		{
			DateTimeOffset sampleDate1 = DateTime.Now.AddSeconds(-5);
			DateTimeOffset sampleDate2 = new DateTime(sampleDate1.Year - 5, 12, 31, 14, 30, 0);

			var styles = new DateTimeFormats[] { DateTimeFormats.Application, DateTimeFormats.System, DateTimeFormats.Universal };

			DateFormats = styles.Select(style => new DateTimeFormatItem(style, sampleDate1, sampleDate2)).ToList();
		}

		private void AddSupportedAppLanguages()
		{
			var appLanguages = ApplicationLanguages.ManifestLanguages
				.Append(string.Empty) // Add default language id
				.Select(language => new AppLanguageItem(language))
				.OrderBy(language => language.LanguagID is not "") // Default language on top
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
				CommandParameter = "Home",
				Tooltip = "Home".GetLocalizedResource()
			});
			await PopulateRecentItems(recentsItem);

			// Ensure recent folders aren't stale since we don't update them with a watcher
			// Then update the items source again to actually include those items
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

				// Add separator if we need one and one wasn't added already
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
				App.Logger.LogInformation(ex, "Could not fetch recent items");
			}

			// Update items source
			AddFlyoutItemsSource = new List<IMenuFlyoutItemViewModel>()
			{
				new MenuFlyoutItemViewModel("Browse".GetLocalizedResource()) { Command = AddPageCommand },
				menu,
			}
			.AsReadOnly();

			return Task.CompletedTask;
		}

		private void PagesOnStartupList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (PagesOnStartupList.Count > 0)
				UserSettingsService.GeneralSettingsService.TabsOnStartupList = PagesOnStartupList.Select((p) => p.Path).ToList();
			else
				UserSettingsService.GeneralSettingsService.TabsOnStartupList = null;
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

		private async Task ChangePage()
		{
			var folderPicker = InitializeWithWindow(new FolderPicker());
			folderPicker.FileTypeFilter.Add("*");
			StorageFolder folder = await folderPicker.PickSingleFolderAsync();

			if (folder is not null)
			{
				if (SelectedPageIndex >= 0)
					PagesOnStartupList[SelectedPageIndex] = new PageOnStartupItem(folder.Path);
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
				PagesOnStartupList.Add(new PageOnStartupItem(path));
		}

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;

				GC.SuppressFinalize(this);
			}
		}

		~GeneralViewModel()
		{
			Dispose();
		}
	}
}
