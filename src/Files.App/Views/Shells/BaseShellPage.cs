// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Filesystem.FilesystemHistory;
using Files.App.Filesystem.Search;
using Files.App.UserControls.MultitaskingControl;
using Files.Backend.Services;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.CompilerServices;
using Windows.System;
using Windows.UI.Core;
using SortDirection = Files.Shared.Enums.SortDirection;

namespace Files.App.Views.Shells
{
	public abstract class BaseShellPage : Page, IShellPage, INotifyPropertyChanged
	{
		public static readonly DependencyProperty NavParamsProperty =
			DependencyProperty.Register(
				"NavParams",
				typeof(NavigationParams),
				typeof(ModernShellPage),
				new PropertyMetadata(null));

		public StorageHistoryHelpers StorageHistoryHelpers { get; }

		protected readonly CancellationTokenSource cancellationTokenSource;

		protected readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

		protected readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		protected readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		protected readonly IUpdateService updateSettingsService = Ioc.Default.GetRequiredService<IUpdateService>();

		protected readonly ICommandManager commands = Ioc.Default.GetRequiredService<ICommandManager>();

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

		protected BaseLayout _ContentPage;
		public BaseLayout ContentPage
		{
			get => _ContentPage;
			set
			{
				if (value != _ContentPage)
				{
					if (_ContentPage is not null)
						_ContentPage.DirectoryPropertiesViewModel.CheckoutRequested -= GitCheckout_Required;

					_ContentPage = value;

					NotifyPropertyChanged(nameof(ContentPage));
					NotifyPropertyChanged(nameof(SlimContentPage));
					if (value is not null)
						_ContentPage.DirectoryPropertiesViewModel.CheckoutRequested += GitCheckout_Required;
				}
			}
		}

		protected IPaneHolder _PaneHolder;
		public IPaneHolder PaneHolder
		{
			get => _PaneHolder;
			set
			{
				if (value != _PaneHolder)
				{
					_PaneHolder = value;

					NotifyPropertyChanged(nameof(PaneHolder));
				}
			}
		}

		protected TabItemArguments _TabItemArguments;
		public TabItemArguments TabItemArguments
		{
			get => _TabItemArguments;
			set
			{
				if (_TabItemArguments != value)
				{
					_TabItemArguments = value;

					ContentChanged?.Invoke(this, value);
				}
			}
		}

		protected bool _IsCurrentInstance = false;
		public bool IsCurrentInstance
		{
			get => _IsCurrentInstance;
			set
			{
				if (_IsCurrentInstance != value)
				{
					_IsCurrentInstance = value;

					if (!value && SlimContentPage is not ColumnViewBrowser)
						ToolbarViewModel.IsEditModeEnabled = false;

					NotifyPropertyChanged(nameof(IsCurrentInstance));
				}
			}
		}

		public SolidColorBrush CurrentInstanceBorderBrush
		{
			get => (SolidColorBrush)GetValue(CurrentInstanceBorderBrushProperty);
			set => SetValue(CurrentInstanceBorderBrushProperty, value);
		}

		public static readonly DependencyProperty CurrentInstanceBorderBrushProperty =
		   DependencyProperty.Register(
			nameof(CurrentInstanceBorderBrush),
			typeof(SolidColorBrush),
			typeof(ModernShellPage),
			new PropertyMetadata(null));

		public event PropertyChangedEventHandler PropertyChanged;

		public event EventHandler<TabItemArguments> ContentChanged;

		public BaseShellPage(CurrentInstanceViewModel instanceViewModel)
		{
			InstanceViewModel = instanceViewModel;
			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired += FolderSettings_LayoutPreferencesUpdateRequired;
			cancellationTokenSource = new CancellationTokenSource();
			FilesystemHelpers = new FilesystemHelpers(this, cancellationTokenSource.Token);
			StorageHistoryHelpers = new StorageHistoryHelpers(new StorageHistoryOperations(this, cancellationTokenSource.Token));

			ToolbarViewModel.InstanceViewModel = InstanceViewModel;

			InitToolbarCommands();

			DisplayFilesystemConsentDialog();

			if (FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft)
				FlowDirection = FlowDirection.RightToLeft;

			ToolbarViewModel.ToolbarPathItemInvoked += ShellPage_NavigationRequested;
			ToolbarViewModel.ToolbarFlyoutOpened += ShellPage_ToolbarFlyoutOpened;
			ToolbarViewModel.ToolbarPathItemLoaded += ShellPage_ToolbarPathItemLoaded;
			ToolbarViewModel.AddressBarTextEntered += ShellPage_AddressBarTextEntered;
			ToolbarViewModel.PathBoxItemDropped += ShellPage_PathBoxItemDropped;

			ToolbarViewModel.RefreshRequested += ShellPage_RefreshRequested;
			ToolbarViewModel.EditModeEnabled += NavigationToolbar_EditModeEnabled;
			ToolbarViewModel.ItemDraggedOverPathItem += ShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted += NavigationToolbar_QuerySubmitted;
			ToolbarViewModel.SearchBox.TextChanged += ShellPage_TextChanged;
			ToolbarViewModel.SearchBox.QuerySubmitted += ShellPage_QuerySubmitted;

			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated += AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated += AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated += AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;

			PointerPressed += CoreWindow_PointerPressed;

			drivesViewModel.PropertyChanged += DrivesManager_PropertyChanged;

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
			// Set focus since selection might occur before the UI finishes updating
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

			InstanceViewModel.GitRepositoryPath = FilesystemViewModel.GitDirectory;

			ContentPage.DirectoryPropertiesViewModel.UpdateGitInfo(
				InstanceViewModel.IsGitRepository, 
				InstanceViewModel.GitRepositoryPath,
				InstanceViewModel.GitBranchName, 
				GitHelpers.GetLocalBranchesNames(InstanceViewModel.GitRepositoryPath));

			ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {directoryItemCountLocalization}";
			ContentPage.UpdateSelectionSize();
		}

		protected void FilesystemViewModel_GitDirectoryUpdated(object sender, EventArgs e)
		{
			InstanceViewModel.UpdateCurrentBranchName();
			ContentPage.DirectoryPropertiesViewModel.UpdateGitInfo(
				InstanceViewModel.IsGitRepository,
				InstanceViewModel.GitRepositoryPath,
				InstanceViewModel.GitBranchName,
				GitHelpers.GetLocalBranchesNames(InstanceViewModel.GitRepositoryPath));
		}

		protected async void GitCheckout_Required(object? sender, string branchName)
		{
			if (!await GitHelpers.Checkout(FilesystemViewModel.GitDirectory, branchName))
				_ContentPage.DirectoryPropertiesViewModel.SelectedBranchIndex = _ContentPage.DirectoryPropertiesViewModel.ActiveBranchIndex;
		}

		protected virtual void Page_Loaded(object sender, RoutedEventArgs e)
		{
			OnNavigationParamsChanged();
			this.Loaded -= Page_Loaded;
		}

		// Some keys are overridden by control built-in defaults(e.g. 'Space').
		// They must be handled here since they're not propagated to KeyboardAccelerator.
		protected void ShellPage_PreviewKeyDown(object sender, KeyRoutedEventArgs args)
		{
			var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shift = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
			var tabInstance =
				CurrentPageType == typeof(DetailsLayoutBrowser) ||
				CurrentPageType == typeof(GridViewBrowser) ||
				CurrentPageType == typeof(ColumnViewBrowser) ||
				CurrentPageType == typeof(ColumnViewBase);

			switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.Key)
			{
				// Ctrl + Space, toggle media playback
				case (true, false, false, true, VirtualKey.Space):
					if (Ioc.Default.GetRequiredService<PreviewPaneViewModel>().PreviewPaneContent is UserControls.FilePreviews.MediaPreview mediaPreviewContent)
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
				SubmitSearch(sender.Query, userSettingsService.GeneralSettingsService.SearchUnindexedItems);
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
					SearchUnindexedItems = userSettingsService.GeneralSettingsService.SearchUnindexedItems
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
				? Constants.UserEnvironmentPaths.HomePath
				: FilesystemViewModel.WorkingDirectory;
		}

		protected void DrivesManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ShowUserConsentOnInit")
				DisplayFilesystemConsentDialog();
		}

		// Ensure that the path bar gets updated for user interaction
		// whenever the path changes.We will get the individual directories from
		// the updated, most-current path and add them to the UI.
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

			var args = new NavigationArguments()
			{
				AssociatedTabInstance = this,
				IsSearchResultPage = true,
				SearchPathParam = FilesystemViewModel.WorkingDirectory,
				SearchQuery = query,
				SearchUnindexedItems = searchUnindexedItems,
			};

			if (this is ColumnShellPage)
				NavigateToPath(FilesystemViewModel.WorkingDirectory, typeof(DetailsLayoutBrowser), args);
			else
				ItemDisplay.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(FilesystemViewModel.WorkingDirectory), args);
		}

		public void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs)
		{
			NavigateToPath(navArgs.NavPathParam, sourcePageType, navArgs);
		}

		public void NavigateToPath(string navigationPath, NavigationArguments? navArgs = null)
		{
			var layout = navigationPath.StartsWith("tag:")
				? typeof(DetailsLayoutBrowser)
				: FolderSettings.GetLayoutType(navigationPath);

			NavigateToPath(navigationPath, layout, navArgs);
		}

		public Task TabItemDragOver(object sender, DragEventArgs e)
		{
			return SlimContentPage?.CommandsViewModel.CommandsModel.DragOver(e);
		}

		public Task TabItemDrop(object sender, DragEventArgs e)
		{
			return SlimContentPage?.CommandsViewModel.CommandsModel.Drop(e);
		}

		public async Task Refresh_Click()
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
			else if (CurrentPageType != typeof(HomePage))
			{
				ToolbarViewModel.CanRefresh = false;
				FilesystemViewModel?.RefreshItems(null);
			}
		}

		public virtual void Back_Click()
		{
			var previousPageContent = ItemDisplay.BackStack[ItemDisplay.BackStack.Count - 1];
			HandleBackForwardRequest(previousPageContent);

			if (previousPageContent.SourcePageType == typeof(HomePage))
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

		public void ResetNavigationStackLayoutMode()
		{
			foreach (PageStackEntry entry in ItemDisplay.BackStack.ToList())
			{
				if (entry.Parameter is NavigationArguments args &&
					args.NavPathParam is not null and not "Home")
				{
					var correctPageType = FolderSettings.GetLayoutType(args.NavPathParam, false);
					if (!entry.SourcePageType.Equals(correctPageType))
					{
						int index = ItemDisplay.BackStack.IndexOf(entry);
						var newEntry = new PageStackEntry(correctPageType, entry.Parameter, entry.NavigationTransitionInfo);
						ItemDisplay.BackStack.RemoveAt(index);
						ItemDisplay.BackStack.Insert(index, newEntry);
					}
				}
			}

			foreach (PageStackEntry entry in ItemDisplay.ForwardStack.ToList())
			{
				if (entry.Parameter is NavigationArguments args &&
					args.NavPathParam is not null and not "Home")
				{
					var correctPageType = FolderSettings.GetLayoutType(args.NavPathParam, false);
					if (!entry.SourcePageType.Equals(correctPageType))
					{
						int index = ItemDisplay.ForwardStack.IndexOf(entry);
						var newEntry = new PageStackEntry(correctPageType, entry.Parameter, entry.NavigationTransitionInfo);
						ItemDisplay.ForwardStack.RemoveAt(index);
						ItemDisplay.ForwardStack.Insert(index, newEntry);
					}
				}
			}
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
						e.PreviousDirectory.Contains(e.Path, StringComparison.Ordinal) &&
						!e.PreviousDirectory.Contains(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
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
			ToolbarViewModel.OpenNewWindowCommand = new AsyncRelayCommand(NavigationHelpers.LaunchNewWindowAsync);
			ToolbarViewModel.CreateNewFileCommand = new RelayCommand<ShellNewEntry>(x => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.File, x, this));
			ToolbarViewModel.PropertiesCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.ShowPropertiesCommand.Execute(null));
			ToolbarViewModel.UpdateCommand = new AsyncRelayCommand(async () => await updateSettingsService.DownloadUpdates());
		}

		protected async Task<BaseLayout> GetContentOrNullAsync()
		{
			// WINUI3: Make sure not to run this synchronously, do not use EnqueueAsync
			var tcs = new TaskCompletionSource<object?>();
			DispatcherQueue.TryEnqueue(() =>
			{
				tcs.SetResult(ItemDisplay.Content);
			});

			return await tcs.Task as BaseLayout;
		}

		protected async Task DisplayFilesystemConsentDialog()
		{
			if (drivesViewModel?.ShowUserConsentOnInit ?? false)
			{
				drivesViewModel.ShowUserConsentOnInit = false;
				await DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					var dialog = DynamicDialogFactory.GetFor_ConsentDialog();
					await SetContentDialogRoot(dialog).ShowAsync(ContentDialogPlacement.Popup);
				});
			}
		}

		protected void SelectSidebarItemFromPath(Type incomingSourcePageType = null)
		{
			if (incomingSourcePageType == typeof(HomePage) && incomingSourcePageType is not null)
				ToolbarViewModel.PathControlDisplayText = "Home".GetLocalizedResource();
		}

		protected void SetLoadingIndicatorForTabs(bool isLoading)
		{
			var multitaskingControls = ((App.Window.Content as Frame).Content as MainPage).ViewModel.MultitaskingControls;

			foreach (var x in multitaskingControls)
				x.SetLoadingIndicatorStatus(x.Items.FirstOrDefault(x => x.Control.TabItemContent == PaneHolder), isLoading);
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

			// Update layout type
			if (pageContent.SourcePageType != typeof(HomePage))
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
			drivesViewModel.PropertyChanged -= DrivesManager_PropertyChanged;

			ToolbarViewModel.ToolbarPathItemInvoked -= ShellPage_NavigationRequested;
			ToolbarViewModel.ToolbarFlyoutOpened -= ShellPage_ToolbarFlyoutOpened;
			ToolbarViewModel.ToolbarPathItemLoaded -= ShellPage_ToolbarPathItemLoaded;
			ToolbarViewModel.AddressBarTextEntered -= ShellPage_AddressBarTextEntered;
			ToolbarViewModel.PathBoxItemDropped -= ShellPage_PathBoxItemDropped;
			ToolbarViewModel.RefreshRequested -= ShellPage_RefreshRequested;
			ToolbarViewModel.EditModeEnabled -= NavigationToolbar_EditModeEnabled;
			ToolbarViewModel.ItemDraggedOverPathItem -= ShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted -= NavigationToolbar_QuerySubmitted;
			ToolbarViewModel.SearchBox.TextChanged -= ShellPage_TextChanged;

			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired -= FolderSettings_LayoutPreferencesUpdateRequired;
			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated -= AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;

			// Prevent weird case of this being null when many tabs are opened/closed quickly
			if (FilesystemViewModel is not null)
			{
				FilesystemViewModel.WorkingDirectoryModified -= ViewModel_WorkingDirectoryModified;
				FilesystemViewModel.ItemLoadStatusChanged -= FilesystemViewModel_ItemLoadStatusChanged;
				FilesystemViewModel.DirectoryInfoUpdated -= FilesystemViewModel_DirectoryInfoUpdated;
				FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
				FilesystemViewModel.OnSelectionRequestedEvent -= FilesystemViewModel_OnSelectionRequestedEvent;
				FilesystemViewModel.GitDirectoryUpdated -= FilesystemViewModel_GitDirectoryUpdated;
				FilesystemViewModel.Dispose();
			}

			if (ItemDisplay.Content is IDisposable disposableContent)
				disposableContent?.Dispose();
		}
	}
}
