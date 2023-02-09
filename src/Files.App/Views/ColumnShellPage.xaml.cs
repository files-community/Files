using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI;
using Files.App.DataModels;
using Files.App.Dialogs;
using Files.App.EventArguments;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.FilesystemHistory;
using Files.App.Filesystem.Search;
using Files.App.Helpers;
using Files.App.UserControls;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels;
using Files.App.Views.LayoutModes;
using Files.Backend.Enums;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using Files.Shared;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using SortDirection = Files.Shared.Enums.SortDirection;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.App.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class ColumnShellPage : Page, IShellPage, INotifyPropertyChanged
	{
		private readonly StorageHistoryHelpers storageHistoryHelpers;
		public IBaseLayout SlimContentPage => ContentPage;
		public IFilesystemHelpers FilesystemHelpers { get; private set; }
		private readonly CancellationTokenSource cancellationTokenSource;

		public bool CanNavigateBackward => false;
		public bool CanNavigateForward => false;

		public FolderSettingsViewModel FolderSettings => InstanceViewModel?.FolderSettings;

		public AppModel AppModel => App.AppModel;

		public bool IsColumnView { get; } = true;

		private readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private readonly IUpdateService updateSettingsService = Ioc.Default.GetRequiredService<IUpdateService>();

		private bool isCurrentInstance = false;
		public bool IsCurrentInstance
		{
			get => isCurrentInstance;
			set
			{
				if (isCurrentInstance != value)
				{
					isCurrentInstance = value;
					if (isCurrentInstance)
					{
						ContentPage?.ItemManipulationModel.FocusFileList();
					}
					//else
					//{
					//    NavigationToolbar.IsEditModeEnabled = false;
					//}
					NotifyPropertyChanged(nameof(IsCurrentInstance));
				}
			}
		}

		public ItemViewModel FilesystemViewModel { get; private set; } = null;
		public CurrentInstanceViewModel InstanceViewModel { get; }
		private BaseLayout contentPage = null;

		public BaseLayout ContentPage
		{
			get => contentPage;
			set
			{
				if (value != contentPage)
				{
					contentPage = value;
					NotifyPropertyChanged(nameof(ContentPage));
					NotifyPropertyChanged(nameof(SlimContentPage));
				}
			}
		}

		private bool isPageMainPane;

		public bool IsPageMainPane
		{
			get => isPageMainPane;
			set
			{
				if (value != isPageMainPane)
				{
					isPageMainPane = value;
					NotifyPropertyChanged(nameof(IsPageMainPane));
				}
			}
		}

		public SolidColorBrush CurrentInstanceBorderBrush
		{
			get { return (SolidColorBrush)GetValue(CurrentInstanceBorderBrushProperty); }
			set { SetValue(CurrentInstanceBorderBrushProperty, value); }
		}

		public static readonly DependencyProperty CurrentInstanceBorderBrushProperty =
			DependencyProperty.Register("CurrentInstanceBorderBrush", typeof(SolidColorBrush), typeof(ColumnShellPage), new PropertyMetadata(null));

		public Type CurrentPageType => ItemDisplayFrame.SourcePageType;

		public ToolbarViewModel ToolbarViewModel { get; } = new ToolbarViewModel();

		public ColumnShellPage()
		{
			InitializeComponent();

			InstanceViewModel = new CurrentInstanceViewModel(FolderLayoutModes.ColumnView);
			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired += FolderSettings_LayoutPreferencesUpdateRequired;
			cancellationTokenSource = new CancellationTokenSource();
			FilesystemHelpers = new FilesystemHelpers(this, cancellationTokenSource.Token);
			storageHistoryHelpers = new StorageHistoryHelpers(new StorageHistoryOperations(this, cancellationTokenSource.Token));

			DisplayFilesystemConsentDialog();

			var flowDirectionSetting = /*
				TODO ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
				Use your ResourceManager instance to create a ResourceContext as below. If you already have a ResourceManager instance,
				replace the new instance created below with correct instance.
				Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
			*/new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"];

			if (flowDirectionSetting == "RTL")
			{
				FlowDirection = FlowDirection.RightToLeft;
			}

			//NavigationToolbar.PathControlDisplayText = "Home".GetLocalizedResource();
			//NavigationToolbar.CanGoBack = false;
			//NavigationToolbar.CanGoForward = false;
			//NavigationToolbar.SearchBox.QueryChanged += ColumnShellPage_QueryChanged;
			//NavigationToolbar.SearchBox.QuerySubmitted += ColumnShellPage_QuerySubmitted;
			//NavigationToolbar.SearchBox.SuggestionChosen += ColumnShellPage_SuggestionChosen;

			ToolbarViewModel.ToolbarPathItemInvoked += ColumnShellPage_NavigationRequested;
			ToolbarViewModel.ToolbarFlyoutOpened += ColumnShellPage_ToolbarFlyoutOpened;
			ToolbarViewModel.ToolbarPathItemLoaded += ColumnShellPage_ToolbarPathItemLoaded;
			ToolbarViewModel.AddressBarTextEntered += ColumnShellPage_AddressBarTextEntered;
			ToolbarViewModel.PathBoxItemDropped += ColumnShellPage_PathBoxItemDropped;

			/*
			TODO UA307 Default back button in the title bar does not exist in WinUI3 apps.
			The tool has generated a custom back button in the MainWindow.xaml.cs file.
			Feel free to edit its position, behavior and use the custom back button instead.
			Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-1#restoring-back-button-functionality
			*/
			ToolbarViewModel.BackRequested += ColumnShellPage_BackNavRequested;
			ToolbarViewModel.UpRequested += ColumnShellPage_UpNavRequested;
			ToolbarViewModel.RefreshRequested += ColumnShellPage_RefreshRequested;
			ToolbarViewModel.ForwardRequested += ColumnShellPage_ForwardNavRequested;
			ToolbarViewModel.EditModeEnabled += NavigationToolbar_EditModeEnabled;
			ToolbarViewModel.ItemDraggedOverPathItem += ColumnShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted += NavigationToolbar_QuerySubmitted;
			ToolbarViewModel.SearchBox.TextChanged += ColumnShellPage_TextChanged;
			ToolbarViewModel.SearchBox.QuerySubmitted += ColumnShellPage_QuerySubmitted;

			ToolbarViewModel.InstanceViewModel = InstanceViewModel;
			//NavToolbarViewModel.RefreshWidgetsRequested += refreshwid;

			InitToolbarCommands();

			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated += AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated += AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated += AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;

			PointerPressed += CoreWindow_PointerPressed;

			/*

			TODO UA307 Default back button in the title bar does not exist in WinUI3 apps.
			The tool has generated a custom back button in the MainWindow.xaml.cs file.
			Feel free to edit its position, behavior and use the custom back button instead.
			Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-1#restoring-back-button-functionality
			*/
			//SystemNavigationManager.GetForCurrentView().BackRequested += ColumnShellPage_BackRequested;

			App.DrivesManager.PropertyChanged += DrivesManager_PropertyChanged;

			PreviewKeyDown += ColumnShellPage_PreviewKeyDown;
		}

		/**
		 * Some keys are overriden by control built-in defaults (e.g. 'Space').
		 * They must be handled here since they're not propagated to KeyboardAccelerator.
		 */
		private async void ColumnShellPage_PreviewKeyDown(object sender, KeyRoutedEventArgs args)
		{
			var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var alt = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
			var tabInstance = CurrentPageType == typeof(DetailsLayoutBrowser) ||
							  CurrentPageType == typeof(GridViewBrowser) ||
							  CurrentPageType == typeof(ColumnViewBrowser) ||
							  CurrentPageType == typeof(ColumnViewBase);

			switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.Key)
			{
				case (true, false, false, true, (VirtualKey)192): // ctrl + ` (accent key), open terminal
																  // Check if there is a folder selected, if not use the current directory.
					string path = FilesystemViewModel.WorkingDirectory;
					if (SlimContentPage?.SelectedItem?.PrimaryItemAttribute == StorageItemTypes.Folder)
					{
						path = SlimContentPage.SelectedItem.ItemPath;
					}

					var terminalStartInfo = new ProcessStartInfo()
					{
						FileName = "wt.exe",
						WorkingDirectory = path
					};
					Process.Start(terminalStartInfo);

					args.Handled = true;
					break;

				case (false, false, false, true, VirtualKey.Space): // space, quick look
																	// handled in `CurrentPageType`::FileList_PreviewKeyDown
					break;

				case (true, false, false, true, VirtualKey.Space): // ctrl + space, toggle media playback
					if (App.PreviewPaneViewModel.PreviewPaneContent is UserControls.FilePreviews.MediaPreview mediaPreviewContent)
					{
						mediaPreviewContent.ViewModel.TogglePlayback();
						args.Handled = true;
					}
					break;
			}
		}

		private async void ColumnShellPage_ToolbarPathItemLoaded(object sender, ToolbarPathItemLoadedEventArgs e)
		{
			await ToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, e.Item, this);
		}

		private async void ColumnShellPage_ToolbarFlyoutOpened(object sender, ToolbarFlyoutOpenedEventArgs e)
		{
			await ToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, (e.OpenedFlyout.Target as FontIcon).DataContext as PathBoxItem, this);
		}

		private void InitToolbarCommands()
		{
			ToolbarViewModel.SelectAllContentPageItemsCommand = new RelayCommand(() => SlimContentPage?.ItemManipulationModel.SelectAllItems());
			ToolbarViewModel.InvertContentPageSelctionCommand = new RelayCommand(() => SlimContentPage?.ItemManipulationModel.InvertSelection());
			ToolbarViewModel.ClearContentPageSelectionCommand = new RelayCommand(() => SlimContentPage?.ItemManipulationModel.ClearSelection());
			ToolbarViewModel.PasteItemsFromClipboardCommand = new RelayCommand(async () => await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this));
			ToolbarViewModel.OpenNewWindowCommand = new AsyncRelayCommand(NavigationHelpers.LaunchNewWindowAsync);
			ToolbarViewModel.OpenNewPaneCommand = new RelayCommand(() => PaneHolder?.OpenPathInNewPane("Home".GetLocalizedResource()));
			ToolbarViewModel.ClosePaneCommand = new RelayCommand(() => PaneHolder?.CloseActivePane());
			ToolbarViewModel.CreateNewFileCommand = new RelayCommand<ShellNewEntry>(x => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.File, x, this));
			ToolbarViewModel.CreateNewFolderCommand = new RelayCommand(() => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.Folder, null, this));
			ToolbarViewModel.CreateNewShortcutCommand = new RelayCommand(() => CreateNewShortcutFromDialog());
			ToolbarViewModel.CopyCommand = new RelayCommand(async () => await UIFilesystemHelpers.CopyItem(this));
			ToolbarViewModel.Rename = new RelayCommand(() => SlimContentPage?.CommandsViewModel.RenameItemCommand.Execute(null));
			ToolbarViewModel.Share = new RelayCommand(() => SlimContentPage?.CommandsViewModel.ShareItemCommand.Execute(null));
			ToolbarViewModel.DeleteCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.DeleteItemCommand.Execute(null));
			ToolbarViewModel.CutCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.CutItemCommand.Execute(null));
			ToolbarViewModel.EmptyRecycleBinCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.EmptyRecycleBinCommand.Execute(null));
			ToolbarViewModel.RestoreRecycleBinCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.RestoreRecycleBinCommand.Execute(null));
			ToolbarViewModel.RestoreSelectionRecycleBinCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.RestoreSelectionRecycleBinCommand.Execute(null));
			ToolbarViewModel.RunWithPowerShellCommand = new RelayCommand(async () => await Win32Helpers.InvokeWin32ComponentAsync("powershell", this, PathNormalization.NormalizePath(SlimContentPage?.SelectedItem.ItemPath)));
			ToolbarViewModel.PropertiesCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.ShowPropertiesCommand.Execute(null));
			ToolbarViewModel.SetAsBackgroundCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.SetAsDesktopBackgroundItemCommand.Execute(null));
			ToolbarViewModel.SetAsLockscreenBackgroundCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.SetAsLockscreenBackgroundItemCommand.Execute(null));
			ToolbarViewModel.SetAsSlideshowCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.SetAsSlideshowItemCommand.Execute(null));
			ToolbarViewModel.ExtractCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.DecompressArchiveCommand.Execute(null));
			ToolbarViewModel.ExtractHereCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.DecompressArchiveHereCommand.Execute(null));
			ToolbarViewModel.ExtractToCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.DecompressArchiveToChildFolderCommand.Execute(null));
			ToolbarViewModel.InstallInfCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.InstallInfDriver.Execute(null));
			ToolbarViewModel.RotateImageLeftCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.RotateImageLeftCommand.Execute(null), () => SlimContentPage?.CommandsViewModel.RotateImageLeftCommand.CanExecute(null) == true);
			ToolbarViewModel.RotateImageRightCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.RotateImageRightCommand.Execute(null), () => SlimContentPage?.CommandsViewModel.RotateImageRightCommand.CanExecute(null) == true);
			ToolbarViewModel.InstallFontCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.InstallFontCommand.Execute(null));
			ToolbarViewModel.UpdateCommand = new AsyncRelayCommand(async () => await updateSettingsService.DownloadUpdates());
		}

		private void FolderSettings_LayoutPreferencesUpdateRequired(object sender, LayoutPreferenceEventArgs e)
		{
		}

		/*
		 * Ensure that the path bar gets updated for user interaction
		 * whenever the path changes. We will get the individual directories from
		 * the updated, most-current path and add them to the UI.
		 */

		public void UpdatePathUIToWorkingDirectory(string newWorkingDir, string singleItemOverride = null)
		{
			if (string.IsNullOrWhiteSpace(singleItemOverride))
			{
				var components = StorageFileExtensions.GetDirectoryPathComponents(newWorkingDir);
				var lastCommonItemIndex = ToolbarViewModel.PathComponents
					.Select((value, index) => new { value, index })
					.LastOrDefault(x => x.index < components.Count && x.value.Path == components[x.index].Path)?.index ?? 0;
				while (ToolbarViewModel.PathComponents.Count > lastCommonItemIndex)
				{
					ToolbarViewModel.PathComponents.RemoveAt(lastCommonItemIndex);
				}
				foreach (var component in components.Skip(lastCommonItemIndex))
				{
					ToolbarViewModel.PathComponents.Add(component);
				}
			}
			else
			{
				ToolbarViewModel.PathComponents.Clear(); // Clear the path UI
				ToolbarViewModel.IsSingleItemOverride = true;
				ToolbarViewModel.PathComponents.Add(new PathBoxItem() { Path = null, Title = singleItemOverride });
			}
		}

		private async void ColumnShellPage_TextChanged(ISearchBox sender, SearchBoxTextChangedEventArgs e)
		{
			if (e.Reason == SearchBoxTextChangeReason.UserInput)
			{
				if (!string.IsNullOrWhiteSpace(sender.Query))
				{
					var search = new FolderSearch
					{
						Query = sender.Query,
						Folder = FilesystemViewModel.WorkingDirectory,
						MaxItemCount = 10,
						SearchUnindexedItems = UserSettingsService.PreferencesSettingsService.SearchUnindexedItems
					};
					sender.SetSuggestions((await search.SearchAsync()).Select(suggestion => new SuggestionModel(suggestion)));
				}
				else
				{
					sender.AddRecentQueries();
				}
			}
		}

		private async void ColumnShellPage_QuerySubmitted(ISearchBox sender, SearchBoxQuerySubmittedEventArgs e)
		{
			if (e.ChosenSuggestion is SuggestionModel item && !string.IsNullOrWhiteSpace(item.ItemPath))
			{
				await NavigationHelpers.OpenPath(item.ItemPath, this);
			}
			else if (e.ChosenSuggestion is null && !string.IsNullOrWhiteSpace(sender.Query))
			{
				SubmitSearch(sender.Query, UserSettingsService.PreferencesSettingsService.SearchUnindexedItems);
			}
		}

		private void ColumnShellPage_RefreshRequested(object sender, EventArgs e)
		{
			Refresh_Click();
		}

		private void ColumnShellPage_UpNavRequested(object sender, EventArgs e)
		{
			Up_Click();
		}

		private void ColumnShellPage_ForwardNavRequested(object sender, EventArgs e)
		{
			Forward_Click();
		}

		private void ColumnShellPage_BackNavRequested(object sender, EventArgs e)
		{
			Back_Click();
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);
			ColumnParams = eventArgs.Parameter as ColumnParam;
			if (ColumnParams?.IsLayoutSwitch ?? false)
				FilesystemViewModel_DirectoryInfoUpdated(this, EventArgs.Empty);
		}

		private void AppSettings_SortDirectionPreferenceUpdated(object sender, SortDirection e)
		{
			FilesystemViewModel?.UpdateSortDirectionStatus();
		}

		private void AppSettings_SortOptionPreferenceUpdated(object sender, SortOption e)
		{
			FilesystemViewModel?.UpdateSortOptionStatus();
		}

		private void AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated(object sender, bool e)
		{
			FilesystemViewModel?.UpdateSortDirectoriesAlongsideFiles();
		}

		private void CoreWindow_PointerPressed(object sender, PointerRoutedEventArgs args)
		{
			if (IsCurrentInstance)
			{
				if (args.GetCurrentPoint(this).Properties.IsXButton1Pressed)
				{
					Back_Click();
				}
				else if (args.GetCurrentPoint(this).Properties.IsXButton2Pressed)
				{
					Forward_Click();
				}
			}
		}

		private async void ColumnShellPage_PathBoxItemDropped(object sender, PathBoxItemDroppedEventArgs e)
		{
			await FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.Path, false, true);
			e.SignalEvent?.Set();
		}

		private void ColumnShellPage_AddressBarTextEntered(object sender, AddressBarTextEnteredEventArgs e)
		{
			ToolbarViewModel.SetAddressBarSuggestions(e.AddressBarTextField, this);
		}

		private void ColumnShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
		{
			this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(e);
		}

		private async void NavigationToolbar_QuerySubmitted(object sender, ToolbarQuerySubmittedEventArgs e)
		{
			await ToolbarViewModel.CheckPathInput(e.QueryText, ToolbarViewModel.PathComponents[ToolbarViewModel.PathComponents.Count - 1].Path, this);
		}

		private void NavigationToolbar_EditModeEnabled(object sender, EventArgs e)
		{
			ToolbarViewModel.ManualEntryBoxLoaded = true;
			ToolbarViewModel.ClickablePathLoaded = false;
			ToolbarViewModel.PathText = string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory)
				? CommonPaths.HomePath
				: FilesystemViewModel.WorkingDirectory;
		}

		private void DrivesManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ShowUserConsentOnInit")
			{
				DisplayFilesystemConsentDialog();
			}
		}

		private async Task<BaseLayout> GetContentOrNullAsync()
		{
			BaseLayout FrameContent = null;
			await DispatcherQueue.EnqueueAsync(() =>
			{
				FrameContent = ItemDisplayFrame.Content as BaseLayout;
			});
			return FrameContent;
		}

		// WINUI3
		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			}
			return contentDialog;
		}

		private async void DisplayFilesystemConsentDialog()
		{
			if (App.DrivesManager?.ShowUserConsentOnInit ?? false)
			{
				App.DrivesManager.ShowUserConsentOnInit = false;
				await DispatcherQueue.EnqueueAsync(async () =>
				{
					DynamicDialog dialog = DynamicDialogFactory.GetFor_ConsentDialog();
					await SetContentDialogRoot(dialog).ShowAsync(ContentDialogPlacement.Popup);
				});
			}
		}

		private ColumnParam columnParams;

		public ColumnParam ColumnParams
		{
			get => columnParams;
			set
			{
				if (value != columnParams)
				{
					columnParams = value;
					if (IsLoaded)
					{
						OnNavigationParamsChanged();
					}
				}
			}
		}

		private void OnNavigationParamsChanged()
		{
			ItemDisplayFrame.Navigate(typeof(ColumnViewBase),
				new NavigationArguments()
				{
					IsSearchResultPage = columnParams.IsSearchResultPage,
					SearchQuery = columnParams.SearchQuery,
					NavPathParam = columnParams.NavPathParam,
					SearchUnindexedItems = columnParams.SearchUnindexedItems,
					SearchPathParam = columnParams.SearchPathParam,
					AssociatedTabInstance = this
				});
		}

		public static readonly DependencyProperty NavParamsProperty =
			DependencyProperty.Register("NavParams", typeof(NavigationParams), typeof(ColumnShellPage), new PropertyMetadata(null));

		private TabItemArguments tabItemArguments;

		public TabItemArguments TabItemArguments
		{
			get => tabItemArguments;
			set
			{
				if (tabItemArguments != value)
				{
					tabItemArguments = value;
					ContentChanged?.Invoke(this, value);
				}
			}
		}

		private IPaneHolder paneHolder;

		public IPaneHolder PaneHolder
		{
			get => paneHolder;
			set
			{
				if (value != paneHolder)
				{
					paneHolder = value;
					NotifyPropertyChanged(nameof(PaneHolder));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public event EventHandler<TabItemArguments> ContentChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			FilesystemViewModel = new ItemViewModel(InstanceViewModel?.FolderSettings);
			FilesystemViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
			FilesystemViewModel.ItemLoadStatusChanged += FilesystemViewModel_ItemLoadStatusChanged;
			FilesystemViewModel.DirectoryInfoUpdated += FilesystemViewModel_DirectoryInfoUpdated;
			FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
			FilesystemViewModel.OnSelectionRequestedEvent += FilesystemViewModel_OnSelectionRequestedEvent;
			OnNavigationParamsChanged();
			Loaded -= Page_Loaded;
		}

		private void FilesystemViewModel_PageTypeUpdated(object sender, PageTypeUpdatedEventArgs e)
		{
			InstanceViewModel.IsPageTypeCloudDrive = e.IsTypeCloudDrive;
		}

		private void FilesystemViewModel_OnSelectionRequestedEvent(object sender, List<ListedItem> e)
		{
			// set focus since selection might occur before the UI finishes updating
			ContentPage.ItemManipulationModel.FocusFileList();
			ContentPage.ItemManipulationModel.SetSelectedItems(e);
		}

		private void FilesystemViewModel_DirectoryInfoUpdated(object sender, EventArgs e)
		{
			if (ContentPage is null)
				return;

			var directoryItemCountLocalization = (FilesystemViewModel.FilesAndFolders.Count == 1)
				? "ItemCount/Text".GetLocalizedResource()
				: "ItemsCount/Text".GetLocalizedResource();

			ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {directoryItemCountLocalization}";
			ContentPage.UpdateSelectionSize();
		}

		private void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
		{
			string value = e.Path;
			if (!string.IsNullOrWhiteSpace(value))
			{
				UpdatePathUIToWorkingDirectory(value);
			}
		}

		private async void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
		{
			ContentPage = await GetContentOrNullAsync();
			if (!ToolbarViewModel.SearchBox.WasQuerySubmitted)
			{
				ToolbarViewModel.SearchBox.Query = string.Empty;
				ToolbarViewModel.IsSearchBoxVisible = false;
			}
			if (ItemDisplayFrame.CurrentSourcePageType == typeof(ColumnViewBase))
			{
				// Reset DataGrid Rows that may be in "cut" command mode
				ContentPage.ResetItemOpacity();
			}
			var parameters = e.Parameter as NavigationArguments;
			TabItemArguments = new TabItemArguments()
			{
				InitialPageType = typeof(ColumnShellPage),
				NavigationArg = parameters.IsSearchResultPage ? parameters.SearchPathParam : parameters.NavPathParam
			};
		}

		private async void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			args.Handled = true;
			var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
			var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
			var alt = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);
			var tabInstance = CurrentPageType == typeof(DetailsLayoutBrowser) ||
							  CurrentPageType == typeof(GridViewBrowser) ||
							  CurrentPageType == typeof(ColumnViewBrowser) ||
							  CurrentPageType == typeof(ColumnViewBase);

			switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.KeyboardAccelerator.Key)
			{
				case (true, false, false, true, VirtualKey.E): // ctrl + e, extract
					{
						if (ToolbarViewModel.CanExtract)
						{
							ToolbarViewModel.ExtractCommand.Execute(null);
						}
						break;
					}

				case (true, false, false, true, VirtualKey.Z): // ctrl + z, undo
					if (!InstanceViewModel.IsPageTypeSearchResults)
					{
						await storageHistoryHelpers.TryUndo();
					}
					break;

				case (true, false, false, true, VirtualKey.Y): // ctrl + y, redo
					if (!InstanceViewModel.IsPageTypeSearchResults)
					{
						await storageHistoryHelpers.TryRedo();
					}
					break;

				case (true, true, false, true, VirtualKey.C):
					{
						SlimContentPage?.CommandsViewModel.CopyPathOfSelectedItemCommand.Execute(null);
						break;
					}

				case (false, false, false, true, VirtualKey.F3): //f3
				case (true, false, false, true, VirtualKey.F): // ctrl + f
					ToolbarViewModel.SwitchSearchBoxVisibility();
					break;

				case (true, true, false, true, VirtualKey.N): // ctrl + shift + n, new item
					if (InstanceViewModel.CanCreateFileInPage)
					{
						var addItemDialogViewModel = new AddItemDialogViewModel();
						await dialogService.ShowDialogAsync(addItemDialogViewModel);
						if (addItemDialogViewModel.ResultType.ItemType == AddItemDialogItemType.Shortcut)
							CreateNewShortcutFromDialog();
						else if (addItemDialogViewModel.ResultType.ItemType != AddItemDialogItemType.Cancel)
							UIFilesystemHelpers.CreateFileFromDialogResultType(
								addItemDialogViewModel.ResultType.ItemType,
								addItemDialogViewModel.ResultType.ItemInfo,
								this);
					}
					break;

				case (false, true, false, true, VirtualKey.Delete): // shift + delete, PermanentDelete
					if (ContentPage.IsItemSelected && !ToolbarViewModel.IsEditModeEnabled && !InstanceViewModel.IsPageTypeSearchResults)
					{
						var items = SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
							item.ItemPath,
							item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
						await FilesystemHelpers.DeleteItemsAsync(items, UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy, true, true);
					}

					break;

				case (true, false, false, true, VirtualKey.C): // ctrl + c, copy
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
					{
						await UIFilesystemHelpers.CopyItem(this);
					}

					break;

				case (true, false, false, true, VirtualKey.V): // ctrl + v, paste
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults && !ToolbarViewModel.SearchHasFocus)
					{
						await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this);
					}

					break;

				case (true, false, false, true, VirtualKey.X): // ctrl + x, cut
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
					{
						UIFilesystemHelpers.CutItem(this);
					}

					break;

				case (true, false, false, true, VirtualKey.A): // ctrl + a, select all
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
					{
						SlimContentPage.ItemManipulationModel.SelectAllItems();
					}

					break;

				case (true, false, false, true, VirtualKey.D): // ctrl + d, delete item
				case (false, false, false, true, VirtualKey.Delete): // delete, delete item
					if (ContentPage.IsItemSelected && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults)
					{
						var items = SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
							item.ItemPath,
							item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
						await FilesystemHelpers.DeleteItemsAsync(items, UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy, false, true);
					}

					break;

				case (true, false, false, true, VirtualKey.P): // ctrl + p, toggle preview pane
					App.PreviewPaneViewModel.IsEnabled = !App.PreviewPaneViewModel.IsEnabled;
					break;

				case (true, false, false, true, VirtualKey.R): // ctrl + r, refresh
					if (ToolbarViewModel.CanRefresh)
					{
						Refresh_Click();
					}
					break;

				case (false, false, true, true, VirtualKey.D): // alt + d, select address bar (english)
				case (true, false, false, true, VirtualKey.L): // ctrl + l, select address bar
					ToolbarViewModel.IsEditModeEnabled = true;
					break;

				case (true, true, false, true, VirtualKey.K): // ctrl + shift + k, duplicate tab
					await NavigationHelpers.OpenPathInNewTab(FilesystemViewModel.WorkingDirectory);
					break;

				case (true, false, false, true, VirtualKey.H): // ctrl + h, toggle hidden folder visibility
					UserSettingsService.FoldersSettingsService.ShowHiddenItems ^= true; // flip bool
					break;

				case (false, false, false, _, VirtualKey.F1): // F1, open Files wiki
					await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.DocumentationUrl));
					break;
			}

			switch (args.KeyboardAccelerator.Key)
			{
				case VirtualKey.F2: //F2, rename
					if (CurrentPageType == typeof(DetailsLayoutBrowser)
						|| CurrentPageType == typeof(GridViewBrowser)
						|| CurrentPageType == typeof(ColumnViewBrowser)
						|| CurrentPageType == typeof(ColumnViewBase))
					{
						if (ContentPage.IsItemSelected)
						{
							ContentPage.ItemManipulationModel.StartRenameItem();
						}
					}
					break;
			}
		}

		public async void Refresh_Click()
		{
			if (InstanceViewModel.IsPageTypeSearchResults)
			{
				ToolbarViewModel.CanRefresh = false;
				var searchInstance = new FolderSearch
				{
					Query = InstanceViewModel.CurrentSearchQuery,
					Folder = FilesystemViewModel.WorkingDirectory,
					ThumbnailSize = InstanceViewModel.FolderSettings.GetIconSize(),
					SearchUnindexedItems = InstanceViewModel.SearchedUnindexedItems
				};
				await FilesystemViewModel.SearchAsync(searchInstance);
			}
			else if (CurrentPageType != typeof(WidgetsPage))
			{
				ToolbarViewModel.CanRefresh = false;
				FilesystemViewModel?.RefreshItems(null);
			}
		}

		public void Back_Click()
		{
			ToolbarViewModel.CanGoBack = false;
			if (ItemDisplayFrame.CanGoBack)
			{
				var previousPageContent = ItemDisplayFrame.BackStack[ItemDisplayFrame.BackStack.Count - 1];
				var previousPageNavPath = previousPageContent.Parameter as NavigationArguments;
				previousPageNavPath.IsLayoutSwitch = false;
				if (previousPageContent.SourcePageType != typeof(WidgetsPage))
				{
					// Update layout type
					InstanceViewModel.FolderSettings.GetLayoutType(previousPageNavPath.IsSearchResultPage ? previousPageNavPath.SearchPathParam : previousPageNavPath.NavPathParam);
				}
				SelectSidebarItemFromPath(previousPageContent.SourcePageType);

				if (previousPageContent.SourcePageType == typeof(WidgetsPage))
				{
					ItemDisplayFrame.GoBack(new EntranceNavigationTransitionInfo());
				}
				else
				{
					ItemDisplayFrame.GoBack();
				}
			}
			else
			{
				this.FindAscendant<ColumnViewBrowser>().NavigateBack();
			}
		}

		public void Forward_Click()
		{
			ToolbarViewModel.CanGoForward = false;
			if (ItemDisplayFrame.CanGoForward)
			{
				var incomingPageContent = ItemDisplayFrame.ForwardStack[ItemDisplayFrame.ForwardStack.Count - 1];
				var incomingPageNavPath = incomingPageContent.Parameter as NavigationArguments;
				incomingPageNavPath.IsLayoutSwitch = false;
				if (incomingPageContent.SourcePageType != typeof(WidgetsPage))
				{
					// Update layout type
					InstanceViewModel.FolderSettings.GetLayoutType(incomingPageNavPath.IsSearchResultPage ? incomingPageNavPath.SearchPathParam : incomingPageNavPath.NavPathParam);
				}
				SelectSidebarItemFromPath(incomingPageContent.SourcePageType);
				ItemDisplayFrame.GoForward();
			}
			else
			{
				this.FindAscendant<ColumnViewBrowser>().NavigateForward();
			}
		}

		public void Up_Click()
		{
			this.FindAscendant<ColumnViewBrowser>().NavigateUp();
		}

		private void SelectSidebarItemFromPath(Type incomingSourcePageType = null)
		{
			if (incomingSourcePageType == typeof(WidgetsPage) && incomingSourcePageType is not null)
			{
				ToolbarViewModel.PathControlDisplayText = "Home".GetLocalizedResource();
			}
		}

		public void Dispose()
		{
			PreviewKeyDown -= ColumnShellPage_PreviewKeyDown;
			PointerPressed -= CoreWindow_PointerPressed;
			//SystemNavigationManager.GetForCurrentView().BackRequested -= ColumnShellPage_BackRequested; //WINUI3
			App.DrivesManager.PropertyChanged -= DrivesManager_PropertyChanged;

			ToolbarViewModel.ToolbarPathItemInvoked -= ColumnShellPage_NavigationRequested;
			ToolbarViewModel.ToolbarFlyoutOpened -= ColumnShellPage_ToolbarFlyoutOpened;
			ToolbarViewModel.ToolbarPathItemLoaded -= ColumnShellPage_ToolbarPathItemLoaded;
			ToolbarViewModel.AddressBarTextEntered -= ColumnShellPage_AddressBarTextEntered;
			ToolbarViewModel.PathBoxItemDropped -= ColumnShellPage_PathBoxItemDropped;
			ToolbarViewModel.BackRequested -= ColumnShellPage_BackNavRequested;
			ToolbarViewModel.UpRequested -= ColumnShellPage_UpNavRequested;
			ToolbarViewModel.RefreshRequested -= ColumnShellPage_RefreshRequested;
			ToolbarViewModel.ForwardRequested -= ColumnShellPage_ForwardNavRequested;
			ToolbarViewModel.EditModeEnabled -= NavigationToolbar_EditModeEnabled;
			ToolbarViewModel.ItemDraggedOverPathItem -= ColumnShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted -= NavigationToolbar_QuerySubmitted;

			ToolbarViewModel.SearchBox.TextChanged -= ColumnShellPage_TextChanged;
			ToolbarViewModel.SearchBox.QuerySubmitted -= ColumnShellPage_QuerySubmitted;

			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired -= FolderSettings_LayoutPreferencesUpdateRequired;
			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated -= AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;

			if (FilesystemViewModel is not null)    // Prevent weird case of this being null when many tabs are opened/closed quickly
			{
				FilesystemViewModel.WorkingDirectoryModified -= ViewModel_WorkingDirectoryModified;
				FilesystemViewModel.ItemLoadStatusChanged -= FilesystemViewModel_ItemLoadStatusChanged;
				FilesystemViewModel.DirectoryInfoUpdated -= FilesystemViewModel_DirectoryInfoUpdated;
				FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
				FilesystemViewModel.OnSelectionRequestedEvent -= FilesystemViewModel_OnSelectionRequestedEvent;
				FilesystemViewModel.Dispose();
			}

			if (ItemDisplayFrame.Content is IDisposable disposableContent)
			{
				disposableContent?.Dispose();
			}
		}

		private void FilesystemViewModel_ItemLoadStatusChanged(object sender, ItemLoadStatusChangedEventArgs e)
		{
			switch (e.Status)
			{
				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting:
					ToolbarViewModel.CanRefresh = false;
					SetLoadingIndicatorForTabs(true);
					break;

				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress:
					var browser = this.FindAscendant<ColumnViewBrowser>();
					ToolbarViewModel.CanGoBack = ItemDisplayFrame.CanGoBack || browser.ParentShellPageInstance.CanNavigateBackward;
					ToolbarViewModel.CanGoForward = ItemDisplayFrame.CanGoForward || browser.ParentShellPageInstance.CanNavigateForward;
					SetLoadingIndicatorForTabs(true);
					break;

				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete:
					SetLoadingIndicatorForTabs(false);
					ToolbarViewModel.CanRefresh = true;
					// Select previous directory
					if (!string.IsNullOrWhiteSpace(e.PreviousDirectory))
					{
						if (e.PreviousDirectory.Contains(e.Path, StringComparison.Ordinal) && !e.PreviousDirectory.Contains(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
						{
							// Remove the WorkingDir from previous dir
							e.PreviousDirectory = e.PreviousDirectory.Replace(e.Path, string.Empty, StringComparison.Ordinal);

							// Get previous dir name
							if (e.PreviousDirectory.StartsWith('\\'))
							{
								e.PreviousDirectory = e.PreviousDirectory.Remove(0, 1);
							}
							if (e.PreviousDirectory.Contains('\\'))
							{
								e.PreviousDirectory = e.PreviousDirectory.Split('\\')[0];
							}

							// Get the first folder and combine it with WorkingDir
							string folderToSelect = string.Format("{0}\\{1}", e.Path, e.PreviousDirectory);

							// Make sure we don't get double \\ in the e.Path
							folderToSelect = folderToSelect.Replace("\\\\", "\\", StringComparison.Ordinal);

							if (folderToSelect.EndsWith('\\'))
							{
								folderToSelect = folderToSelect.Remove(folderToSelect.Length - 1, 1);
							}

							ListedItem itemToSelect = FilesystemViewModel.FilesAndFolders.Where((item) => item.ItemPath == folderToSelect).FirstOrDefault();

							if (itemToSelect is not null && ContentPage is not null)
							{
								ContentPage.ItemManipulationModel.SetSelectedItem(itemToSelect);
								ContentPage.ItemManipulationModel.ScrollIntoView(itemToSelect);
							}
						}
					}
					break;
			}
		}

		private void SetLoadingIndicatorForTabs(bool isLoading)
		{
			var multitaskingControls = ((App.Window.Content as Frame).Content as MainPage).ViewModel.MultitaskingControls;

			foreach (var x in multitaskingControls)
			{
				x.SetLoadingIndicatorStatus(x.Items.FirstOrDefault(x => x.Control.TabItemContent == PaneHolder), isLoading);
			}
		}

		public Task TabItemDragOver(object sender, DragEventArgs e) => SlimContentPage?.CommandsViewModel.CommandsModel.DragOver(e);

		public Task TabItemDrop(object sender, DragEventArgs e) => SlimContentPage?.CommandsViewModel.CommandsModel.Drop(e);

		public void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs)
		{
			NavigateToPath(navArgs.NavPathParam, sourcePageType, navArgs);
		}

		public void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null)
		{
			this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(navigationPath, sourcePageType, navArgs);
		}

		public void NavigateToPath(string navigationPath, NavigationArguments navArgs = null)
		{
			NavigateToPath(navigationPath, FolderSettings.GetLayoutType(navigationPath), navArgs);
		}

		public void NavigateHome()
		{
			throw new NotImplementedException("Can't show Home page in Column View");
		}

		public void RemoveLastPageFromBackStack()
		{
			ItemDisplayFrame.BackStack.Remove(ItemDisplayFrame.BackStack.Last());
		}

		public void SubmitSearch(string query, bool searchUnindexedItems)
		{
			FilesystemViewModel.CancelSearch();
			InstanceViewModel.CurrentSearchQuery = query;
			InstanceViewModel.SearchedUnindexedItems = searchUnindexedItems;
			ItemDisplayFrame.Navigate(typeof(ColumnViewBase), new NavigationArguments()
			{
				AssociatedTabInstance = this,
				IsSearchResultPage = true,
				SearchPathParam = FilesystemViewModel.WorkingDirectory,
				SearchQuery = query,
				SearchUnindexedItems = searchUnindexedItems,
			});
			//this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(null, typeof(ColumnViewBase), navArgs);
		}

		private async void CreateNewShortcutFromDialog()
			=> await UIFilesystemHelpers.CreateShortcutFromDialogAsync(this);
	}
}