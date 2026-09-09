// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Core;
using WinRT;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.Views.Shells
{
	public abstract class BaseShellPage : Page, IShellPage, INotifyPropertyChanged
	{
		private DispatcherQueueTimer? _updateDateDisplayTimer;

		private DateTimeFormats _lastDateTimeFormats;

		private Task _gitFetch = Task.CompletedTask;

		private CancellationTokenSource _gitFetchToken = new CancellationTokenSource();

		public static readonly DependencyProperty NavParamsProperty =
			DependencyProperty.Register(
				"NavParams",
				typeof(NavigationParams),
				typeof(BaseShellPage),
				new PropertyMetadata(null));

		public StorageHistoryHelpers StorageHistoryHelpers { get; }

		protected readonly CancellationTokenSource cancellationTokenSource;
		private bool isDisposed;

		protected readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

		protected readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		protected readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		protected readonly IUpdateService updateSettingsService = Ioc.Default.GetRequiredService<IUpdateService>();

		protected readonly ICommandManager commands = Ioc.Default.GetRequiredService<ICommandManager>();

		public NavigationToolbarViewModel ToolbarViewModel { get; } = new NavigationToolbarViewModel();

		public IBaseLayoutPage? SlimContentPage => ContentPage;

		public IFilesystemHelpers FilesystemHelpers { get; protected set; }

		public Type CurrentPageType => ItemDisplay.SourcePageType;

		public LayoutPreferencesManager FolderSettings => InstanceViewModel.FolderSettings;

		public AppModel AppModel => App.AppModel;

		protected abstract Frame ItemDisplay { get; }

		public virtual bool CanNavigateForward => ItemDisplay.CanGoForward;

		public virtual bool CanNavigateBackward => ItemDisplay.CanGoBack;

		public bool IsColumnView => SlimContentPage is ColumnsLayoutPage;

		public virtual IList<PageStackEntry> ForwardStack => ItemDisplay.ForwardStack;

		public virtual IList<PageStackEntry> BackwardStack => ItemDisplay.BackStack;

		public ShellViewModel? ShellViewModel { get; protected set; }

		public CurrentInstanceViewModel InstanceViewModel { get; }

		protected BaseLayoutPage? _ContentPage;
		public BaseLayoutPage? ContentPage
		{
			get => _ContentPage;
			set
			{
				if (value != _ContentPage)
				{
					if (_ContentPage is not null)
						_ContentPage.StatusBarViewModel.CheckoutRequested -= GitCheckout_Required;

					_ContentPage = value;

					NotifyPropertyChanged(nameof(ContentPage));
					NotifyPropertyChanged(nameof(SlimContentPage));
					if (value is not null)
						value.StatusBarViewModel.CheckoutRequested += GitCheckout_Required;
				}
			}
		}

		protected IShellPanesPage? _PaneHolder;
		public IShellPanesPage? PaneHolder
		{
			get => _PaneHolder;
			set
			{
				if (value != _PaneHolder)
				{
					if (_PaneHolder is not null)
						_PaneHolder.PropertyChanged -= PaneHolder_PropertyChanged;

					_PaneHolder = value;

					if (_PaneHolder is not null)
						_PaneHolder.PropertyChanged += PaneHolder_PropertyChanged;

					NotifyPropertyChanged(nameof(PaneHolder));
				}
			}
		}

		public bool IsStatusBarVisible =>
			userSettingsService.AppearanceSettingsService.ShowStatusBar &&
			CurrentPageType != typeof(HomePage) &&
			CurrentPageType != typeof(ReleaseNotesPage) &&
			CurrentPageType != typeof(SettingsPage) &&
			(PaneHolder is null || !PaneHolder.IsMultiPaneActive || Equals(PaneHolder.ActivePane, this));

		protected TabBarItemParameter? _TabItemArguments;
		public TabBarItemParameter? TabBarItemParameter
		{
			get => _TabItemArguments;
			set
			{
				if (_TabItemArguments != value)
				{
					_TabItemArguments = value;

					ContentChanged?.Invoke(
						this,
						value ?? throw new InvalidOperationException("The tab content arguments cannot be cleared."));
				}
			}
		}

		protected TaskCompletionSource _IsCurrentInstanceTCS = new();
		protected bool _IsCurrentInstance = false;
		public bool IsCurrentInstance
		{
			get => _IsCurrentInstance;
			set
			{
				if (_IsCurrentInstance != value)
				{
					_IsCurrentInstance = value;

					if (value)
					{
						_IsCurrentInstanceTCS.TrySetResult();
						_updateDateDisplayTimer?.Start();
						ShellViewModel?.UpdateDateDisplay();
					}
					else
					{
						_IsCurrentInstanceTCS = new();
						_updateDateDisplayTimer?.Stop();
					}

					NotifyPropertyChanged(nameof(IsCurrentInstance));

					// Update background to show off the focused shell page
					if (!IsColumnView)
						VisualStateManager.GoToState(this, value ? "ShellBackgroundFocusOnState" : "ShellBackgroundFocusOffState", true);
				}
			}
		}

		public virtual bool IsCurrentPane => IsCurrentInstance;

		public virtual Task WhenIsCurrent() => _IsCurrentInstanceTCS.Task;

		public event PropertyChangedEventHandler? PropertyChanged;

		public event EventHandler<TabBarItemParameter>? ContentChanged;

		public BaseShellPage(CurrentInstanceViewModel instanceViewModel)
		{
			InstanceViewModel = instanceViewModel;
			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired += FolderSettings_LayoutPreferencesUpdateRequired;
			cancellationTokenSource = new CancellationTokenSource();
			FilesystemHelpers = new FilesystemHelpers(this, cancellationTokenSource.Token);
			StorageHistoryHelpers = new StorageHistoryHelpers(new StorageHistoryOperations(this, cancellationTokenSource.Token));

			ToolbarViewModel.InstanceViewModel = InstanceViewModel;

			InitToolbarCommands();

			_ = DisplayFilesystemConsentDialogAsync();

			if (AppLanguageHelper.IsPreferredLanguageRtl)
				FlowDirection = FlowDirection.RightToLeft;

			ToolbarViewModel.ToolbarPathItemInvoked += ShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxItemDropped += ShellPage_PathBoxItemDropped;

			ToolbarViewModel.ItemDraggedOverPathItem += ShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted += NavigationToolbar_QuerySubmitted;

			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated += AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated += AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated += AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortFilesFirstPreferenceUpdated += AppSettings_SortFilesFirstPreferenceUpdated;

			PointerPressed += CoreWindow_PointerPressed;

			drivesViewModel.PropertyChanged += DrivesManager_PropertyChanged;

			PreviewKeyDown += ShellPage_PreviewKeyDown;

			GitHelpers.GitFetchCompleted += FilesystemViewModel_GitDirectoryUpdated;

			userSettingsService.AppearanceSettingsService.PropertyChanged += AppearanceSettingsService_PropertyChanged;

			_updateDateDisplayTimer = DispatcherQueue.CreateTimer();
			_updateDateDisplayTimer.Interval = TimeSpan.FromSeconds(1);
			_updateDateDisplayTimer.Tick += UpdateDateDisplayTimer_Tick;
			_lastDateTimeFormats = userSettingsService.GeneralSettingsService.DateTimeFormat;

			App.AppModel.PropertyChanged += AppModel_PropertyChanged;
		}

		private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(AppModel.IsMainWindowClosed))
				return;

			// Ticks dispatched during dispatcher queue shutdown crash in CoreMessaging
			if (App.AppModel.IsMainWindowClosed)
				_updateDateDisplayTimer?.Stop();
			else if (IsCurrentInstance)
				_updateDateDisplayTimer?.Start();
		}

		protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void PaneHolder_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IShellPanesPage.IsMultiPaneActive) or nameof(IShellPanesPage.ActivePane))
				NotifyPropertyChanged(nameof(IsStatusBarVisible));
		}

		private void AppearanceSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IAppearanceSettingsService.ShowStatusBar))
				NotifyPropertyChanged(nameof(IsStatusBarVisible));
		}

		protected void FilesystemViewModel_PageTypeUpdated(object? sender, PageTypeUpdatedEventArgs e)
		{
			InstanceViewModel.IsPageTypeCloudDrive = e.IsTypeCloudDrive;
		}

		protected void FilesystemViewModel_OnSelectionRequestedEvent(object? sender, List<ListedItem> e)
		{
			var contentPage = ContentPage
				?? throw new InvalidOperationException("The content page is not available for selection.");

			// Raised by the directory watcher, which can fire while the user is typing in the
			// omnibar - don't yank focus in that case (the omnibar's TryCancel doesn't help here
			// because FocusFileList -> ListViewItem.Focus uses FocusState.Keyboard).
			if (!UIHelpers.IsTextInputFocused(XamlRoot))
				contentPage.ItemManipulationModel.FocusFileList();
			contentPage.ItemManipulationModel.SetSelectedItems(e);
		}

		protected async void FilesystemViewModel_DirectoryInfoUpdated(object? sender, EventArgs e)
		{
			if (ContentPage is null)
				return;

			var shellViewModel = this.GetRequiredShellViewModel();

			var directoryItemCountLocalization = Strings.Items.GetLocalizedFormatResource(shellViewModel.FilesAndFolders.Count);

			BranchItem? headBranch = InstanceViewModel.IsGitRepository
					? await GitHelpers.GetRepositoryHead(InstanceViewModel.GitRepositoryPath)
					: null;

			shellViewModel = this.GetRequiredShellViewModel();

			if (InstanceViewModel.GitRepositoryPath != shellViewModel.GitDirectory)
			{
				InstanceViewModel.GitRepositoryPath = shellViewModel.GitDirectory;
				InstanceViewModel.IsGitRepository = shellViewModel.IsValidGitDirectory;

				InstanceViewModel.GitBranchName = headBranch is not null
					? headBranch.Name
					: string.Empty;

				var isGitFetchCanceled = false;
				if (!_gitFetch.IsCompleted)
				{
					var canceledFetch = _gitFetch;
					var canceledFetchToken = _gitFetchToken;
					canceledFetchToken.Cancel();
					_ = canceledFetch.ContinueWith(
						_ => canceledFetchToken.Dispose(),
						CancellationToken.None,
						TaskContinuationOptions.ExecuteSynchronously,
						TaskScheduler.Default);
					_gitFetchToken = new CancellationTokenSource();
					isGitFetchCanceled = true;
				}
				if (InstanceViewModel.IsGitRepository && (!GitHelpers.IsExecutingGitAction || isGitFetchCanceled))
				{
					_gitFetch = GitHelpers.FetchOriginAsync(InstanceViewModel.GitRepositoryPath, cancellationToken: _gitFetchToken.Token);
				}
			}

			var contentPage = ContentPage;
			if (contentPage is null)
				return;

			if (!GitHelpers.IsExecutingGitAction)
			{
				contentPage.StatusBarViewModel.UpdateGitInfo(
					InstanceViewModel.IsGitRepository,
					InstanceViewModel.GitRepositoryPath,
					headBranch);
			}

			contentPage.StatusBarViewModel.DirectoryItemCount = $"{shellViewModel.FilesAndFolders.Count} {directoryItemCountLocalization}";
			contentPage.InfoPaneViewModel.DirectoryItemCount = $"{shellViewModel.FilesAndFolders.Count} {directoryItemCountLocalization}";
			contentPage.UpdateSelectionSize();
		}

		protected async void FilesystemViewModel_GitDirectoryUpdated(object? sender, EventArgs e)
		{
			if (GitHelpers.IsExecutingGitAction)
				return;

			var head = InstanceViewModel.IsGitRepository
				? await GitHelpers.GetRepositoryHead(InstanceViewModel.GitRepositoryPath)
				: null;

			InstanceViewModel.GitBranchName = head is not null
				? head.Name
				: string.Empty;

			ContentPage?.StatusBarViewModel.UpdateGitInfo(
				InstanceViewModel.IsGitRepository,
				InstanceViewModel.GitRepositoryPath,
				head);
		}

		protected async void GitCheckout_Required(object? sender, string branchName)
		{
			var shellViewModel = this.GetRequiredShellViewModel();

			if (!await GitHelpers.Checkout(shellViewModel.GitDirectory, branchName))
			{
				var contentPage = ContentPage
					?? throw new InvalidOperationException("The content page is not available after Git checkout failed.");

				contentPage.StatusBarViewModel.ShowLocals = true;
				contentPage.StatusBarViewModel.SelectedBranchIndex = StatusBarViewModel.ACTIVE_BRANCH_INDEX;
			}
			else
			{
				var contentPage = ContentPage
					?? throw new InvalidOperationException("The content page is not available after Git checkout.");

				contentPage.StatusBarViewModel.UpdateGitInfo(
					InstanceViewModel.IsGitRepository,
					InstanceViewModel.GitRepositoryPath,
					await GitHelpers.GetRepositoryHead(InstanceViewModel.GitRepositoryPath));
			}
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
				CurrentPageType == typeof(DetailsLayoutPage) ||
				CurrentPageType == typeof(GridLayoutPage) ||
				CurrentPageType == typeof(ColumnsLayoutPage) ||
				CurrentPageType == typeof(ColumnLayoutPage);

			switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.Key)
			{
				// Ctrl + Space, toggle media playback
				case (true, false, false, true, VirtualKey.Space):
					if (Ioc.Default.GetRequiredService<InfoPaneViewModel>().PreviewPaneContent is UserControls.FilePreviews.MediaPreview mediaPreviewContent)
					{
						mediaPreviewContent.ViewModel.TogglePlayback();
						args.Handled = true;
					}
					break;
			}
		}

		protected void AppSettings_SortDirectionPreferenceUpdated(object? sender, SortDirection e)
		{
			ShellViewModel?.UpdateSortDirectionStatusAsync();
		}

		protected void AppSettings_SortOptionPreferenceUpdated(object? sender, SortOption e)
		{
			ShellViewModel?.UpdateSortOptionStatusAsync();
		}

		protected void AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated(object? sender, bool e)
		{
			ShellViewModel?.UpdateSortDirectoriesAlongsideFilesAsync();
		}

		protected void AppSettings_SortFilesFirstPreferenceUpdated(object? sender, bool e)
		{
			ShellViewModel?.UpdateSortFilesFirstAsync();
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
			if (e.Package is not { } package || e.Path is not { } destination)
			{
				e.SignalEvent?.Set();
				return;
			}

			await FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, package, destination, false, true);
			e.SignalEvent?.Set();
		}

		protected async void NavigationToolbar_QuerySubmitted(object sender, ToolbarQuerySubmittedEventArgs e)
		{
			var queryText = e.QueryText
				?? throw new InvalidOperationException("The submitted navigation query is missing.");
			await ToolbarViewModel.CheckPathInputAsync(queryText, ToolbarViewModel.PathComponents.LastOrDefault()?.Path, this);
		}

		protected async void DrivesManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ShowUserConsentOnInit")
				await DisplayFilesystemConsentDialogAsync();
		}

		private volatile CancellationTokenSource? cts;

		// Ensure that the path bar gets updated for user interaction
		// whenever the path changes.We will get the individual directories from
		// the updated, most-current path and add them to the UI.
		public async Task UpdatePathUIToWorkingDirectoryAsync(string? newWorkingDir, string? singleItemOverride = null)
		{
			if (string.IsNullOrWhiteSpace(singleItemOverride))
			{
				if (newWorkingDir is null)
					throw new InvalidOperationException("The working directory is not available for the path bar.");

				cts = new CancellationTokenSource();

				var components = await StorageFileExtensions.GetDirectoryPathComponentsWithDisplayNameAsync(newWorkingDir);

				// Cancel if overrided by single item
				if (cts.IsCancellationRequested)
					return;

				// Guard against a rare race where a native CollectionChanged subscriber (e.g. the
				// bound BreadcrumbBar) is in a torn-down state during navigation and throws NRE.
				try
				{
					ToolbarViewModel.PathComponents.Clear();
					foreach (var component in components)
						ToolbarViewModel.PathComponents.Add(component);
				}
				catch (NullReferenceException)
				{
				}
			}
			else
			{
				cts?.Cancel();

				try
				{
					// Clear the path UI
					ToolbarViewModel.PathComponents.Clear();
					ToolbarViewModel.IsSingleItemOverride = true;
					ToolbarViewModel.PathComponents.Add(
						new()
						{
							Path = null,
							Title = singleItemOverride,
							ChevronToolTip = string.Format(Strings.BreadcrumbBarChevronButtonToolTip.GetLocalizedResource(), singleItemOverride),
						});
				}
				catch (NullReferenceException)
				{
				}
			}
		}

		public void SubmitSearch(string query)
		{
			var shellViewModel = this.GetRequiredShellViewModel();

			shellViewModel.CancelSearch();
			InstanceViewModel.CurrentSearchQuery = query;

			var args = new NavigationArguments()
			{
				AssociatedTabInstance = this,
				IsSearchResultPage = true,
				SearchPathParam = shellViewModel.WorkingDirectory,
				SearchQuery = query,
			};

			var layout = InstanceViewModel.FolderSettings.GetLayoutType(shellViewModel.WorkingDirectory);

			if (layout == typeof(ColumnsLayoutPage))
				NavigateToPath(shellViewModel.WorkingDirectory, typeof(DetailsLayoutPage), args);
			else
				NavigateToPath(shellViewModel.WorkingDirectory, layout, args);
		}

		public void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs)
		{
			NavigateToPath(navArgs.NavPathParam, sourcePageType, navArgs);
		}

		public void NavigateToPath(string navigationPath, NavigationArguments? navArgs = null)
		{
			var layout = FolderSettings.GetLayoutType(navigationPath);

			// Don't use Columns Layout for displaying tags
			if (navigationPath.StartsWith("tag:") && layout == typeof(ColumnsLayoutPage))
				layout = typeof(DetailsLayoutPage);

			NavigateToPath(navigationPath, layout, navArgs);
		}

		public Task TabItemDragOver(object sender, DragEventArgs e)
		{
			return SlimContentPage?.CommandsViewModel?.DragOverAsync(e)
				?? Task.CompletedTask;
		}

		public Task TabItemDrop(object sender, DragEventArgs e)
		{
			return SlimContentPage?.CommandsViewModel?.DropAsync(e)
				?? Task.CompletedTask;
		}

		public async Task RefreshIfNoWatcherExistsAsync()
		{
			var shellViewModel = this.GetRequiredShellViewModel();
			if (shellViewModel.HasNoWatcher)
				await Refresh_Click();
		}

		public async Task Refresh_Click()
		{
			if (InstanceViewModel.IsPageTypeSearchResults)
			{
				var shellViewModel = this.GetRequiredShellViewModel();
				var searchQuery = InstanceViewModel.CurrentSearchQuery;
				if (searchQuery is null)
				{
					var tabArguments = TabBarItemParameter
						?? throw new InvalidOperationException("The tab navigation arguments are not available for search refresh.");
					searchQuery = (string?)tabArguments.NavigationParameter;
				}

				ToolbarViewModel.CanRefresh = false;
				var searchInstance = new FolderSearch
				{
					Query = searchQuery,
					Folder = shellViewModel.WorkingDirectory,
				};

				await shellViewModel.SearchAsync(searchInstance);
			}
			else if (CurrentPageType != typeof(HomePage))
			{
				var shellViewModel = this.GetRequiredShellViewModel();
				ToolbarViewModel.CanRefresh = false;
				shellViewModel.RefreshItems(null);
			}
			else if (ItemDisplay.Content is HomePage homePage)
			{
				ToolbarViewModel.CanRefresh = false;
				await homePage.ViewModel.RefreshWidgetProperties();
				ToolbarViewModel.CanRefresh = true;
			}
		}

		public virtual void Back_Click()
		{
			var previousPageContent = ItemDisplay.BackStack[ItemDisplay.BackStack.Count - 1];
			HandleBackForwardRequest(previousPageContent);

			if (ItemDisplay.CanGoBack)
				ItemDisplay.GoBack();
		}

		public virtual void Forward_Click()
		{
			var incomingPageContent = ItemDisplay.ForwardStack[ItemDisplay.ForwardStack.Count - 1];
			HandleBackForwardRequest(incomingPageContent);

			if (ItemDisplay.CanGoForward)
				ItemDisplay.GoForward();
		}

		public void ResetNavigationStackLayoutMode()
		{
			foreach (PageStackEntry entry in ItemDisplay.BackStack.ToList())
			{
				if (entry.Parameter is NavigationArguments args &&
					args.NavPathParam is not null and not "Home" &&
					args.NavPathParam is not null and not "ReleaseNotes" &&
					args.NavPathParam is not null and not "Settings")
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
					args.NavPathParam is not null and not "Home" &&
					args.NavPathParam is not null and not "ReleaseNotes" &&
					args.NavPathParam is not null and not "Settings")
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
			ItemDisplay.BackStack.Remove(ItemDisplay.BackStack.LastOrDefault());
		}

		public void RaiseContentChanged(IShellPage instance, TabBarItemParameter args)
		{
			ContentChanged?.Invoke(instance, args);
		}

		protected void FilesystemViewModel_ItemLoadStatusChanged(object? sender, ItemLoadStatusChangedEventArgs e)
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
					if (SlimContentPage is ColumnsLayoutPage browser)
					{
						columnCanNavigateBackward = browser.ParentShellPageInstance?.CanNavigateBackward ?? false;
						columnCanNavigateForward = browser.ParentShellPageInstance?.CanNavigateForward ?? false;
					}
					ToolbarViewModel.CanGoBack = ItemDisplay.CanGoBack || columnCanNavigateBackward;
					ToolbarViewModel.CanGoForward = ItemDisplay.CanGoForward || columnCanNavigateForward;
					SetLoadingIndicatorForTabs(true);
					break;
				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete:
					SetLoadingIndicatorForTabs(false);

					if (ContentPage is not null)
						ContentPage.ItemManipulationModel.ScrollToTop();

					ToolbarViewModel.CanRefresh = true;
					// Select previous directory
					var path = e.Path;
					if (path is not null &&
						!string.IsNullOrWhiteSpace(e.PreviousDirectory) &&
						e.PreviousDirectory.Contains(
							path,
							StringComparison.Ordinal) &&
						!e.PreviousDirectory.Contains(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
					{
						// Remove the WorkingDir from previous dir
						e.PreviousDirectory = e.PreviousDirectory.Replace(path, string.Empty, StringComparison.Ordinal);

						var isNetwork = path.StartsWith("\\\\", StringComparison.Ordinal);
						var isFtp = FtpHelpers.IsFtpPath(path);
						var separator = isFtp ? "/" : "\\";

						// Get previous dir name
						if (e.PreviousDirectory.StartsWith(separator))
							e.PreviousDirectory = e.PreviousDirectory.Remove(0, 1);
						if (e.PreviousDirectory.Contains(separator))
							e.PreviousDirectory = e.PreviousDirectory.Split(separator)[0];

						// Get the first folder and combine it with WorkingDir
						string folderToSelect = path + separator + e.PreviousDirectory;

						// Make sure we don't get double separators in the e.Path
						folderToSelect = folderToSelect.Replace(separator + separator, separator, StringComparison.Ordinal);

						if (isNetwork)
							folderToSelect = separator + folderToSelect;
						else if (isFtp)
							folderToSelect = folderToSelect.Replace(":/", "://", StringComparison.Ordinal);

						if (folderToSelect.EndsWith(separator))
							folderToSelect = folderToSelect.Remove(folderToSelect.Length - 1, 1);

						var shellViewModel = this.GetRequiredShellViewModel();
						var itemToSelect = shellViewModel.FilesAndFolders.ToList().FirstOrDefault((item) => item.ItemPath == folderToSelect);

						if (itemToSelect is not null && ContentPage is not null && userSettingsService.FoldersSettingsService.ScrollToPreviousFolderWhenNavigatingUp)
						{
							ContentPage.ItemManipulationModel.SetSelectedItem(itemToSelect);
							ContentPage.ItemManipulationModel.ScrollIntoView(itemToSelect);
						}
					}
					break;
			}
		}

		private void FolderSettings_LayoutPreferencesUpdateRequired(object? sender, LayoutPreferenceEventArgs e)
		{
			if (ShellViewModel is null)
				return;

			LayoutPreferencesManager.SetLayoutPreferencesForPath(ShellViewModel.WorkingDirectory, e.LayoutPreference);
			if (e.IsAdaptiveLayoutUpdateRequired)
				AdaptiveLayoutHelpers.ApplyAdaptativeLayout(InstanceViewModel.FolderSettings, ShellViewModel.FilesAndFolders.ToList());
		}

		protected virtual void ViewModel_WorkingDirectoryModified(object? sender, WorkingDirectoryModifiedEventArgs e)
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
			ToolbarViewModel.CreateNewFileCommand = new RelayCommand<ShellNewEntry>(x => _ = UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(AddItemDialogItemType.File, x, this));
			ToolbarViewModel.UpdateCommand = new AsyncRelayCommand(async () => await updateSettingsService.DownloadUpdatesAsync());
		}

		protected async Task<BaseLayoutPage?> GetContentOrNullAsync()
		{
			// WINUI3: Make sure not to run this synchronously, do not use EnqueueAsync
			var tcs = new TaskCompletionSource<object?>();
			DispatcherQueue.TryEnqueue(() =>
			{
				tcs.SetResult(ItemDisplay.Content);
			});

			return await tcs.Task as BaseLayoutPage;
		}

		protected async Task DisplayFilesystemConsentDialogAsync()
		{
			if (drivesViewModel?.ShowUserConsentOnInit ?? false)
			{
				drivesViewModel.ShowUserConsentOnInit = false;
				await DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					var dialog = DynamicDialogFactory.GetFor_ConsentDialog();

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await dialog.ShowAsync();
				});
			}
		}

		protected void SelectSidebarItemFromPath(Type? incomingSourcePageType = null)
		{
			if (incomingSourcePageType == typeof(HomePage))
				ToolbarViewModel.PathControlDisplayText = Strings.Home.GetLocalizedResource();
		}

		[DynamicWindowsRuntimeCast(typeof(Frame))]
		protected void SetLoadingIndicatorForTabs(bool isLoading)
		{
			try
			{
				var mainPage = (MainWindow.Instance.Content as Frame)?.Content as MainPage
					?? throw new InvalidOperationException("The main page is not available for updating tab loading indicators.");

				foreach (var tabBar in mainPage.ViewModel.MultitaskingControls)
				{
					if (tabBar.Items.FirstOrDefault(item => item.TabItemContent == PaneHolder) is { } tabItem)
						tabBar.SetLoadingIndicatorStatus(tabItem, isLoading);
				}
			}
			catch (COMException)
			{

			}
		}

		// WINUI3
		protected static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;
			return contentDialog;
		}

		private void HandleBackForwardRequest(PageStackEntry pageContent)
		{
			var incomingPageNavPath = pageContent.Parameter as NavigationArguments;
			if (incomingPageNavPath is not null)
				incomingPageNavPath.IsLayoutSwitch = false;

			// Update layout type
			if (pageContent.SourcePageType != typeof(HomePage))
			{
				var navigationArguments = incomingPageNavPath
					?? throw new InvalidOperationException("The navigation entry does not contain navigation arguments.");
				var path = navigationArguments.IsSearchResultPage
					? navigationArguments.SearchPathParam
					: navigationArguments.NavPathParam;
				InstanceViewModel.FolderSettings.GetLayoutType(path);
			}

			SelectSidebarItemFromPath(pageContent.SourcePageType);
		}

		public abstract void Up_Click();

		public abstract void NavigateHome();

		public abstract void NavigateToReleaseNotes();

		public abstract void NavigateToSettings(string? selectItem = null);

		public abstract void NavigateToPath(string? navigationPath, Type? sourcePageType, NavigationArguments? navArgs = null);

		private void UpdateDateDisplayTimer_Tick(object sender, object e)
		{
			if (App.AppModel.IsMainWindowClosed)
				return;

			if (userSettingsService.GeneralSettingsService.DateTimeFormat != _lastDateTimeFormats)
			{
				_lastDateTimeFormats = userSettingsService.GeneralSettingsService.DateTimeFormat;
				ShellViewModel?.UpdateDateDisplay();
			}
			else if (userSettingsService.GeneralSettingsService.DateTimeFormat == DateTimeFormats.Application)
			{
				ShellViewModel?.UpdateDateDisplay();
			}
		}

		public virtual void Dispose()
		{
			if (isDisposed)
				return;

			isDisposed = true;
			cancellationTokenSource.Cancel();

			PreviewKeyDown -= ShellPage_PreviewKeyDown;
			PointerPressed -= CoreWindow_PointerPressed;
			drivesViewModel.PropertyChanged -= DrivesManager_PropertyChanged;
			userSettingsService.AppearanceSettingsService.PropertyChanged -= AppearanceSettingsService_PropertyChanged;

			if (_PaneHolder is not null)
				_PaneHolder.PropertyChanged -= PaneHolder_PropertyChanged;

			ToolbarViewModel.ToolbarPathItemInvoked -= ShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxItemDropped -= ShellPage_PathBoxItemDropped;
			ToolbarViewModel.ItemDraggedOverPathItem -= ShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted -= NavigationToolbar_QuerySubmitted;
			ToolbarViewModel.Dispose();

			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired -= FolderSettings_LayoutPreferencesUpdateRequired;
			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated -= AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortFilesFirstPreferenceUpdated -= AppSettings_SortFilesFirstPreferenceUpdated;

			// Prevent weird case of this being null when many tabs are opened/closed quickly
			if (ShellViewModel is not null)
			{
				ShellViewModel.WorkingDirectoryModified -= ViewModel_WorkingDirectoryModified;
				ShellViewModel.ItemLoadStatusChanged -= FilesystemViewModel_ItemLoadStatusChanged;
				ShellViewModel.DirectoryInfoUpdated -= FilesystemViewModel_DirectoryInfoUpdated;
				ShellViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
				ShellViewModel.OnSelectionRequestedEvent -= FilesystemViewModel_OnSelectionRequestedEvent;
				ShellViewModel.GitDirectoryUpdated -= FilesystemViewModel_GitDirectoryUpdated;
				ShellViewModel.Dispose();
			}

			if (ItemDisplay.Content is IDisposable disposableContent)
				disposableContent.Dispose();

			ContentPage = null!;
			ItemDisplay.Content = null;

			GitHelpers.GitFetchCompleted -= FilesystemViewModel_GitDirectoryUpdated;

			App.AppModel.PropertyChanged -= AppModel_PropertyChanged;

			_updateDateDisplayTimer?.Stop();
			if (_updateDateDisplayTimer is not null)
			{
				_updateDateDisplayTimer.Tick -= UpdateDateDisplayTimer_Tick;
				_updateDateDisplayTimer = null;
			}
			cancellationTokenSource.Dispose();
			var gitFetchToken = _gitFetchToken;
			gitFetchToken.Cancel();
			_ = _gitFetch.ContinueWith(
				_ => gitFetchToken.Dispose(),
				CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
		}
	}
}
