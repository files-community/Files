using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.DataModels;
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
using Files.Shared;
using Files.Shared.Enums;
using Microsoft.UI.Input;
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

namespace Files.App.Views
{
	public abstract class BaseShellPage : Page, IShellPage, INotifyPropertyChanged
	{
		public static readonly DependencyProperty NavParamsProperty =
			DependencyProperty.Register("NavParams", typeof(NavigationParams), typeof(ModernShellPage), new PropertyMetadata(null));

		protected readonly StorageHistoryHelpers storageHistoryHelpers;

		protected readonly CancellationTokenSource cancellationTokenSource;

		protected readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		protected readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		protected readonly IUpdateService updateSettingsService = Ioc.Default.GetRequiredService<IUpdateService>();

		public ToolbarViewModel ToolbarViewModel { get; } = new ToolbarViewModel();

		public IBaseLayout SlimContentPage => ContentPage;

		public IFilesystemHelpers FilesystemHelpers { get; protected set; }

		public Type CurrentPageType => ItemDisplay.SourcePageType;

		public FolderSettingsViewModel FolderSettings => InstanceViewModel.FolderSettings;

		public AppModel AppModel => App.AppModel;

		protected abstract Frame ItemDisplay { get; }

		public abstract bool CanNavigateForward { get; }

		public abstract bool CanNavigateBackward { get; }

		public bool IsColumnView => SlimContentPage is ColumnViewBrowser;

		public ItemViewModel FilesystemViewModel { get; protected set; }

		public CurrentInstanceViewModel InstanceViewModel { get; }

		protected BaseLayout contentPage;
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

		protected bool isPageMainPane;
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

		protected IPaneHolder paneHolder;
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

		protected TabItemArguments tabItemArguments;
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

		protected bool isCurrentInstance = false;
		public bool IsCurrentInstance
		{
			get => isCurrentInstance;
			set
			{
				if (isCurrentInstance != value)
				{
					isCurrentInstance = value;
					if (isCurrentInstance)
						ContentPage?.ItemManipulationModel.FocusFileList();
					else if (SlimContentPage is not ColumnViewBrowser)
						ToolbarViewModel.IsEditModeEnabled = false;
					NotifyPropertyChanged(nameof(IsCurrentInstance));
				}
			}
		}

		public SolidColorBrush CurrentInstanceBorderBrush
		{
			get { return (SolidColorBrush)GetValue(CurrentInstanceBorderBrushProperty); }
			set { SetValue(CurrentInstanceBorderBrushProperty, value); }
		}

		public static readonly DependencyProperty CurrentInstanceBorderBrushProperty =
		   DependencyProperty.Register("CurrentInstanceBorderBrush", typeof(SolidColorBrush), typeof(ModernShellPage), new PropertyMetadata(null));

		public event PropertyChangedEventHandler PropertyChanged;

		public event EventHandler<TabItemArguments> ContentChanged;

		public BaseShellPage(CurrentInstanceViewModel instanceViewModel)
		{
			InstanceViewModel = instanceViewModel;
			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired += FolderSettings_LayoutPreferencesUpdateRequired;
			cancellationTokenSource = new CancellationTokenSource();
			FilesystemHelpers = new FilesystemHelpers(this, cancellationTokenSource.Token);
			storageHistoryHelpers = new StorageHistoryHelpers(new StorageHistoryOperations(this, cancellationTokenSource.Token));

			ToolbarViewModel.InstanceViewModel = InstanceViewModel;

			InitToolbarCommands();

			DisplayFilesystemConsentDialog();

			/*TODO ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
			  Use your ResourceManager instance to create a ResourceContext as below.If you already have a ResourceManager instance,
			  replace the new instance created below with correct instance.
			  Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
			*/
			var flowDirectionSetting = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"];

			if (flowDirectionSetting == "RTL")
				FlowDirection = FlowDirection.RightToLeft;

			ToolbarViewModel.ToolbarPathItemInvoked += ShellPage_NavigationRequested;
			ToolbarViewModel.ToolbarFlyoutOpened += ShellPage_ToolbarFlyoutOpened;
			ToolbarViewModel.ToolbarPathItemLoaded += ShellPage_ToolbarPathItemLoaded;
			ToolbarViewModel.AddressBarTextEntered += ShellPage_AddressBarTextEntered;
			ToolbarViewModel.PathBoxItemDropped += ShellPage_PathBoxItemDropped;

			ToolbarViewModel.BackRequested += ShellPage_BackNavRequested;
			ToolbarViewModel.UpRequested += ShellPage_UpNavRequested;
			ToolbarViewModel.RefreshRequested += ShellPage_RefreshRequested;
			ToolbarViewModel.ForwardRequested += ShellPage_ForwardNavRequested;
			ToolbarViewModel.EditModeEnabled += NavigationToolbar_EditModeEnabled;
			ToolbarViewModel.ItemDraggedOverPathItem += ShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted += NavigationToolbar_QuerySubmitted;
			ToolbarViewModel.SearchBox.TextChanged += ShellPage_TextChanged;
			ToolbarViewModel.SearchBox.QuerySubmitted += ShellPage_QuerySubmitted;

			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated += AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated += AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated += AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;

			this.PointerPressed += CoreWindow_PointerPressed;

			/*
			TODO UA307 Default back button in the title bar does not exist in WinUI3 apps.
			The tool has generated a custom back button in the MainWindow.xaml.cs file.
			Feel free to edit its position, behavior and use the custom back button instead.
			Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-1#restoring-back-button-functionality
			*/

			App.DrivesManager.PropertyChanged += DrivesManager_PropertyChanged;

			PreviewKeyDown += ShellPage_PreviewKeyDown;
		}

		protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void FilesystemViewModel_PageTypeUpdated(object sender, PageTypeUpdatedEventArgs e)
		{
			InstanceViewModel.IsPageTypeCloudDrive = e.IsTypeCloudDrive;
		}

		protected void FilesystemViewModel_OnSelectionRequestedEvent(object sender, List<ListedItem> e)
		{
			// set focus since selection might occur before the UI finishes updating
			ContentPage.ItemManipulationModel.FocusFileList();
			ContentPage.ItemManipulationModel.SetSelectedItems(e);
		}

		protected void FilesystemViewModel_DirectoryInfoUpdated(object sender, EventArgs e)
		{
			if (ContentPage is null)
				return;

			var directoryItemCountLocalization = (FilesystemViewModel.FilesAndFolders.Count == 1)
				? "ItemCount/Text".GetLocalizedResource()
				: "ItemsCount/Text".GetLocalizedResource();

			ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {directoryItemCountLocalization}";
			ContentPage.UpdateSelectionSize();
		}

		protected virtual void Page_Loaded(object sender, RoutedEventArgs e)
		{
			OnNavigationParamsChanged();
			this.Loaded -= Page_Loaded;
		}

		/**
		 * Some keys are overriden by control built-in defaults (e.g. 'Space').
		 * They must be handled here since they're not propagated to KeyboardAccelerator.
		 */
		protected void ShellPage_PreviewKeyDown(object sender, KeyRoutedEventArgs args)
		{
			var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shift = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
			var tabInstance = CurrentPageType == typeof(DetailsLayoutBrowser) ||
							  CurrentPageType == typeof(GridViewBrowser) ||
							  CurrentPageType == typeof(ColumnViewBrowser) ||
							  CurrentPageType == typeof(ColumnViewBase);

			switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.Key)
			{
				// Ctrl + ` (accent key), open terminal
				case (true, false, false, true, (VirtualKey)192):

					// Check if there is a folder selected, if not use the current directory.
					string path = FilesystemViewModel.WorkingDirectory;
					if (SlimContentPage?.SelectedItem?.PrimaryItemAttribute == StorageItemTypes.Folder)
						path = SlimContentPage.SelectedItem.ItemPath;

					var terminalStartInfo = new ProcessStartInfo()
					{
						FileName = "wt.exe",
						Arguments = $"-d {path}",
						Verb = shift ? "runas" : "",
						UseShellExecute = true
					};
					DispatcherQueue.TryEnqueue(() => Process.Start(terminalStartInfo));

					args.Handled = true;

					break;

				// Ctrl + space, toggle media playback
				case (true, false, false, true, VirtualKey.Space):

					if (App.PreviewPaneViewModel.PreviewPaneContent is UserControls.FilePreviews.MediaPreview mediaPreviewContent)
					{
						mediaPreviewContent.ViewModel.TogglePlayback();
						args.Handled = true;
					}

					break;
			}
		}

		protected async void ShellPage_QuerySubmitted(ISearchBox sender, SearchBoxQuerySubmittedEventArgs e)
		{
			if (e.ChosenSuggestion is SuggestionModel item && !string.IsNullOrWhiteSpace(item.ItemPath))
				await NavigationHelpers.OpenPath(item.ItemPath, this);
			else if (e.ChosenSuggestion is null && !string.IsNullOrWhiteSpace(sender.Query))
				SubmitSearch(sender.Query, userSettingsService.PreferencesSettingsService.SearchUnindexedItems);
		}

		protected async void ShellPage_TextChanged(ISearchBox sender, SearchBoxTextChangedEventArgs e)
		{
			if (e.Reason != SearchBoxTextChangeReason.UserInput)
				return;
			if (!string.IsNullOrWhiteSpace(sender.Query))
			{
				var search = new FolderSearch
				{
					Query = sender.Query,
					Folder = FilesystemViewModel.WorkingDirectory,
					MaxItemCount = 10,
					SearchUnindexedItems = userSettingsService.PreferencesSettingsService.SearchUnindexedItems
				};
				sender.SetSuggestions((await search.SearchAsync()).Select(suggestion => new SuggestionModel(suggestion)));
			}
			else
			{
				sender.AddRecentQueries();
			}
		}

		protected void ShellPage_RefreshRequested(object sender, EventArgs e)
		{
			Refresh_Click();
		}

		protected void ShellPage_UpNavRequested(object sender, EventArgs e)
		{
			Up_Click();
		}

		protected void ShellPage_ForwardNavRequested(object sender, EventArgs e)
		{
			Forward_Click();
		}

		protected void ShellPage_BackNavRequested(object sender, EventArgs e)
		{
			Back_Click();
		}

		protected void AppSettings_SortDirectionPreferenceUpdated(object sender, SortDirection e)
		{
			FilesystemViewModel?.UpdateSortDirectionStatus();
		}

		protected void AppSettings_SortOptionPreferenceUpdated(object sender, SortOption e)
		{
			FilesystemViewModel?.UpdateSortOptionStatus();
		}

		protected void AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated(object sender, bool e)
		{
			FilesystemViewModel?.UpdateSortDirectoriesAlongsideFiles();
		}

		protected void CoreWindow_PointerPressed(object sender, PointerRoutedEventArgs args)
		{
			if (!IsCurrentInstance)
				return;
			if (args.GetCurrentPoint(this).Properties.IsXButton1Pressed)
				Back_Click();
			else if (args.GetCurrentPoint(this).Properties.IsXButton2Pressed)
				Forward_Click();
		}

		protected async void ShellPage_PathBoxItemDropped(object sender, PathBoxItemDroppedEventArgs e)
		{
			await FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.Path, false, true);
			e.SignalEvent?.Set();
		}

		protected void ShellPage_AddressBarTextEntered(object sender, AddressBarTextEnteredEventArgs e)
		{
			ToolbarViewModel.SetAddressBarSuggestions(e.AddressBarTextField, this);
		}

		protected async void ShellPage_ToolbarPathItemLoaded(object sender, ToolbarPathItemLoadedEventArgs e)
		{
			await ToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, e.Item, this);
		}

		protected async void ShellPage_ToolbarFlyoutOpened(object sender, ToolbarFlyoutOpenedEventArgs e)
		{
			await ToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, (e.OpenedFlyout.Target as FontIcon).DataContext as PathBoxItem, this);
		}

		protected async void NavigationToolbar_QuerySubmitted(object sender, ToolbarQuerySubmittedEventArgs e)
		{
			await ToolbarViewModel.CheckPathInput(e.QueryText, ToolbarViewModel.PathComponents.LastOrDefault()?.Path, this);
		}

		protected void NavigationToolbar_EditModeEnabled(object sender, EventArgs e)
		{
			ToolbarViewModel.ManualEntryBoxLoaded = true;
			ToolbarViewModel.ClickablePathLoaded = false;
			ToolbarViewModel.PathText = string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory)
				? CommonPaths.HomePath
				: FilesystemViewModel.WorkingDirectory;
		}

		protected void DrivesManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ShowUserConsentOnInit")
				DisplayFilesystemConsentDialog();
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
					ToolbarViewModel.PathComponents.RemoveAt(lastCommonItemIndex);

				foreach (var component in components.Skip(lastCommonItemIndex))
					ToolbarViewModel.PathComponents.Add(component);
			}
			else
			{
				// Clear the path UI
				ToolbarViewModel.PathComponents.Clear();
				ToolbarViewModel.IsSingleItemOverride = true;
				ToolbarViewModel.PathComponents.Add(new PathBoxItem() { Path = null, Title = singleItemOverride });
			}
		}

		public void SubmitSearch(string query, bool searchUnindexedItems)
		{
			FilesystemViewModel.CancelSearch();
			InstanceViewModel.CurrentSearchQuery = query;
			InstanceViewModel.SearchedUnindexedItems = searchUnindexedItems;
			ItemDisplay.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(FilesystemViewModel.WorkingDirectory), new NavigationArguments()
			{
				AssociatedTabInstance = this,
				IsSearchResultPage = true,
				SearchPathParam = FilesystemViewModel.WorkingDirectory,
				SearchQuery = query,
				SearchUnindexedItems = searchUnindexedItems,
			});
		}

		public void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs)
		{
			NavigateToPath(navArgs.NavPathParam, sourcePageType, navArgs);
		}

		public void NavigateToPath(string navigationPath, NavigationArguments? navArgs = null)
		{
			NavigateToPath(navigationPath, FolderSettings.GetLayoutType(navigationPath), navArgs);
		}

		public Task TabItemDragOver(object sender, DragEventArgs e)
		{
			return SlimContentPage?.CommandsViewModel.CommandsModel.DragOver(e);
		}

		public Task TabItemDrop(object sender, DragEventArgs e)
		{
			return SlimContentPage?.CommandsViewModel.CommandsModel.Drop(e);
		}

		public async void Refresh_Click()
		{
			if (InstanceViewModel.IsPageTypeSearchResults)
			{
				ToolbarViewModel.CanRefresh = false;
				var searchInstance = new FolderSearch
				{
					Query = InstanceViewModel.CurrentSearchQuery ?? (string)TabItemArguments.NavigationArg,
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

		public virtual void Back_Click()
		{
			var previousPageContent = ItemDisplay.BackStack[ItemDisplay.BackStack.Count - 1];
			HandleBackForwardRequest(previousPageContent);

			if (previousPageContent.SourcePageType == typeof(WidgetsPage))
				ItemDisplay.GoBack(new EntranceNavigationTransitionInfo());
			else
				ItemDisplay.GoBack();
		}

		public virtual void Forward_Click()
		{
			var incomingPageContent = ItemDisplay.ForwardStack[ItemDisplay.ForwardStack.Count - 1];
			HandleBackForwardRequest(incomingPageContent);
			ItemDisplay.GoForward();
		}

		public void RemoveLastPageFromBackStack()
		{
			ItemDisplay.BackStack.Remove(ItemDisplay.BackStack.Last());
		}

		public void RaiseContentChanged(IShellPage instance, TabItemArguments args)
		{
			ContentChanged?.Invoke(instance, args);
		}

		protected void FilesystemViewModel_ItemLoadStatusChanged(object sender, ItemLoadStatusChangedEventArgs e)
		{
			switch (e.Status)
			{
				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting:
					ToolbarViewModel.CanRefresh = false;
					SetLoadingIndicatorForTabs(true);
					break;

				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress:
					var columnCanNavigateBackward = false;
					var columnCanNavigateForward = false;
					if (SlimContentPage is ColumnViewBrowser browser)
					{
						columnCanNavigateBackward = browser.ParentShellPageInstance.CanNavigateBackward;
						columnCanNavigateForward = browser.ParentShellPageInstance.CanNavigateForward;
					}
					ToolbarViewModel.CanGoBack = ItemDisplay.CanGoBack || columnCanNavigateBackward;
					ToolbarViewModel.CanGoForward = ItemDisplay.CanGoForward || columnCanNavigateForward;
					SetLoadingIndicatorForTabs(true);
					break;

				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete:
					SetLoadingIndicatorForTabs(false);
					ToolbarViewModel.CanRefresh = true;
					// Select previous directory
					if (!string.IsNullOrWhiteSpace(e.PreviousDirectory) &&
						e.PreviousDirectory.Contains(e.Path, StringComparison.Ordinal) && !e.PreviousDirectory.Contains(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
					{
						// Remove the WorkingDir from previous dir
						e.PreviousDirectory = e.PreviousDirectory.Replace(e.Path, string.Empty, StringComparison.Ordinal);

						// Get previous dir name
						if (e.PreviousDirectory.StartsWith('\\'))
							e.PreviousDirectory = e.PreviousDirectory.Remove(0, 1);
						if (e.PreviousDirectory.Contains('\\'))
							e.PreviousDirectory = e.PreviousDirectory.Split('\\')[0];

						// Get the first folder and combine it with WorkingDir
						string folderToSelect = string.Format("{0}\\{1}", e.Path, e.PreviousDirectory);

						// Make sure we don't get double \\ in the e.Path
						folderToSelect = folderToSelect.Replace("\\\\", "\\", StringComparison.Ordinal);

						if (folderToSelect.EndsWith('\\'))
							folderToSelect = folderToSelect.Remove(folderToSelect.Length - 1, 1);

						var itemToSelect = FilesystemViewModel.FilesAndFolders.Where((item) => item.ItemPath == folderToSelect).FirstOrDefault();

						if (itemToSelect is not null && ContentPage is not null)
						{
							ContentPage.ItemManipulationModel.SetSelectedItem(itemToSelect);
							ContentPage.ItemManipulationModel.ScrollIntoView(itemToSelect);
						}
					}
					break;
			}
		}

		protected virtual void FolderSettings_LayoutPreferencesUpdateRequired(object sender, LayoutPreferenceEventArgs e)
		{
		}

		protected virtual void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
		{
		}

		protected virtual void OnNavigationParamsChanged()
		{
		}

		protected virtual void ShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
		{
		}

		protected void InitToolbarCommands()
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
			ToolbarViewModel.PlayAllCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.PlayAllCommand.Execute(null));
		}

		protected async Task<BaseLayout> GetContentOrNullAsync()
		{
			// WINUI3: make sure not to run this synchronously, do not use EnqueueAsync
			var tcs = new TaskCompletionSource<object?>();
			DispatcherQueue.TryEnqueue(() =>
			{
				tcs.SetResult(ItemDisplay.Content);
			});
			return await tcs.Task as BaseLayout;
		}

		protected async void DisplayFilesystemConsentDialog()
		{
			if (App.DrivesManager?.ShowUserConsentOnInit ?? false)
			{
				App.DrivesManager.ShowUserConsentOnInit = false;
				await DispatcherQueue.EnqueueAsync(async () =>
				{
					var dialog = DynamicDialogFactory.GetFor_ConsentDialog();
					await SetContentDialogRoot(dialog).ShowAsync(ContentDialogPlacement.Popup);
				});
			}
		}

		protected void SelectSidebarItemFromPath(Type incomingSourcePageType = null)
		{
			if (incomingSourcePageType == typeof(WidgetsPage) && incomingSourcePageType is not null)
				ToolbarViewModel.PathControlDisplayText = "Home".GetLocalizedResource();
		}

		protected void SetLoadingIndicatorForTabs(bool isLoading)
		{
			var multitaskingControls = ((App.Window.Content as Frame).Content as MainPage).ViewModel.MultitaskingControls;

			foreach (var x in multitaskingControls)
				x.SetLoadingIndicatorStatus(x.Items.FirstOrDefault(x => x.Control.TabItemContent == PaneHolder), isLoading);
		}

		protected async void CreateNewShortcutFromDialog()
		{
			await UIFilesystemHelpers.CreateShortcutFromDialogAsync(this);
		}

		// WINUI3
		protected static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			return contentDialog;
		}

		private void HandleBackForwardRequest(PageStackEntry pageContent)
		{
			var incomingPageNavPath = pageContent.Parameter as NavigationArguments;
			incomingPageNavPath.IsLayoutSwitch = false;
			if (pageContent.SourcePageType != typeof(WidgetsPage)) // Update layout type
				InstanceViewModel.FolderSettings.GetLayoutType(incomingPageNavPath.IsSearchResultPage ? incomingPageNavPath.SearchPathParam : incomingPageNavPath.NavPathParam);
			SelectSidebarItemFromPath(pageContent.SourcePageType);
		}

		public abstract void Up_Click();

		public abstract void NavigateHome();

		public abstract void NavigateToPath(string? navigationPath, Type? sourcePageType, NavigationArguments? navArgs = null);

		public virtual void Dispose()
		{
			PreviewKeyDown -= ShellPage_PreviewKeyDown;
			PointerPressed -= CoreWindow_PointerPressed;
			App.DrivesManager.PropertyChanged -= DrivesManager_PropertyChanged;

			ToolbarViewModel.ToolbarPathItemInvoked -= ShellPage_NavigationRequested;
			ToolbarViewModel.ToolbarFlyoutOpened -= ShellPage_ToolbarFlyoutOpened;
			ToolbarViewModel.ToolbarPathItemLoaded -= ShellPage_ToolbarPathItemLoaded;
			ToolbarViewModel.AddressBarTextEntered -= ShellPage_AddressBarTextEntered;
			ToolbarViewModel.PathBoxItemDropped -= ShellPage_PathBoxItemDropped;
			ToolbarViewModel.BackRequested -= ShellPage_BackNavRequested;
			ToolbarViewModel.UpRequested -= ShellPage_UpNavRequested;
			ToolbarViewModel.RefreshRequested -= ShellPage_RefreshRequested;
			ToolbarViewModel.ForwardRequested -= ShellPage_ForwardNavRequested;
			ToolbarViewModel.EditModeEnabled -= NavigationToolbar_EditModeEnabled;
			ToolbarViewModel.ItemDraggedOverPathItem -= ShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted -= NavigationToolbar_QuerySubmitted;
			ToolbarViewModel.SearchBox.TextChanged -= ShellPage_TextChanged;

			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired -= FolderSettings_LayoutPreferencesUpdateRequired;
			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated -= AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;

			if (FilesystemViewModel is not null) // Prevent weird case of this being null when many tabs are opened/closed quickly
			{
				FilesystemViewModel.WorkingDirectoryModified -= ViewModel_WorkingDirectoryModified;
				FilesystemViewModel.ItemLoadStatusChanged -= FilesystemViewModel_ItemLoadStatusChanged;
				FilesystemViewModel.DirectoryInfoUpdated -= FilesystemViewModel_DirectoryInfoUpdated;
				FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
				FilesystemViewModel.OnSelectionRequestedEvent -= FilesystemViewModel_OnSelectionRequestedEvent;
				FilesystemViewModel.Dispose();
			}

			if (ItemDisplay.Content is IDisposable disposableContent)
				disposableContent?.Dispose();
		}
	}
}