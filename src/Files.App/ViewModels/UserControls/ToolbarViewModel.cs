// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.Shared.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.IO;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Text;

namespace Files.App.ViewModels.UserControls
{
	public class ToolbarViewModel : ObservableObject, IAddressToolbar, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private readonly IDialogService _dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

		public IUpdateService UpdateService { get; } = Ioc.Default.GetService<IUpdateService>()!;

		public ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public delegate void ToolbarPathItemInvokedEventHandler(object sender, PathNavigationEventArgs e);

		public delegate void ToolbarFlyoutOpenedEventHandler(object sender, ToolbarFlyoutOpenedEventArgs e);

		public delegate void ToolbarPathItemLoadedEventHandler(object sender, ToolbarPathItemLoadedEventArgs e);

		public delegate void AddressBarTextEnteredEventHandler(object sender, AddressBarTextEnteredEventArgs e);

		public delegate void PathBoxItemDroppedEventHandler(object sender, PathBoxItemDroppedEventArgs e);

		public event ToolbarPathItemInvokedEventHandler? ToolbarPathItemInvoked;

		public event ToolbarFlyoutOpenedEventHandler? ToolbarFlyoutOpened;

		public event ToolbarPathItemLoadedEventHandler? ToolbarPathItemLoaded;

		public event IAddressToolbar.ItemDraggedOverPathItemEventHandler? ItemDraggedOverPathItem;

		public event EventHandler? EditModeEnabled;

		public event IAddressToolbar.ToolbarQuerySubmittedEventHandler? PathBoxQuerySubmitted;

		public event AddressBarTextEnteredEventHandler? AddressBarTextEntered;

		public event PathBoxItemDroppedEventHandler? PathBoxItemDropped;

		public event EventHandler? RefreshRequested;

		public event EventHandler? RefreshWidgetsRequested;

		public ObservableCollection<PathBoxItem> PathComponents { get; } = new();

		private bool _isCommandPaletteOpen;
		public bool IsCommandPaletteOpen
		{
			get => _isCommandPaletteOpen;
			set => SetProperty(ref _isCommandPaletteOpen, value);
		}

		private bool isUpdating;
		public bool IsUpdating
		{
			get => isUpdating;
			set => SetProperty(ref isUpdating, value);
		}

		private bool isUpdateAvailable;
		public bool IsUpdateAvailable
		{
			get => isUpdateAvailable;
			set => SetProperty(ref isUpdateAvailable, value);
		}

		private string? releaseNotes;
		public string? ReleaseNotes
		{
			get => releaseNotes;
			set => SetProperty(ref releaseNotes, value);
		}

		private bool isReleaseNotesVisible;
		public bool IsReleaseNotesVisible
		{
			get => isReleaseNotesVisible;
			set => SetProperty(ref isReleaseNotesVisible, value);
		}

		private bool canCopyPathInPage;
		public bool CanCopyPathInPage
		{
			get => canCopyPathInPage;
			set => SetProperty(ref canCopyPathInPage, value);
		}

		private bool canGoBack;
		public bool CanGoBack
		{
			get => canGoBack;
			set => SetProperty(ref canGoBack, value);
		}

		private bool canGoForward;
		public bool CanGoForward
		{
			get => canGoForward;
			set => SetProperty(ref canGoForward, value);
		}

		private bool canNavigateToParent;
		public bool CanNavigateToParent
		{
			get => canNavigateToParent;
			set => SetProperty(ref canNavigateToParent, value);
		}

		private bool previewPaneEnabled;
		public bool PreviewPaneEnabled
		{
			get => previewPaneEnabled;
			set => SetProperty(ref previewPaneEnabled, value);
		}

		private bool canRefresh;
		public bool CanRefresh
		{
			get => canRefresh;
			set => SetProperty(ref canRefresh, value);
		}

		private string searchButtonGlyph = "\uE721";
		public string SearchButtonGlyph
		{
			get => searchButtonGlyph;
			set => SetProperty(ref searchButtonGlyph, value);
		}

		private bool isSearchBoxVisible;
		public bool IsSearchBoxVisible
		{
			get => isSearchBoxVisible;
			set
			{
				if (SetProperty(ref isSearchBoxVisible, value))
					SearchButtonGlyph = value ? "\uE711" : "\uE721";
			}
		}

		private string? pathText;
		public string? PathText
		{
			get => pathText;
			set
			{
				pathText = value;

				OnPropertyChanged(nameof(PathText));
			}
		}

		public ObservableCollection<NavigationBarSuggestionItem> NavigationBarSuggestions = new();

		private CurrentInstanceViewModel instanceViewModel;
		public CurrentInstanceViewModel InstanceViewModel
		{
			get => instanceViewModel;
			set
			{
				if (instanceViewModel?.FolderSettings is not null)
					instanceViewModel.FolderSettings.PropertyChanged -= FolderSettings_PropertyChanged;

				if (SetProperty(ref instanceViewModel, value) && instanceViewModel?.FolderSettings is not null)
				{
					FolderSettings_PropertyChanged(this, new PropertyChangedEventArgs(nameof(FolderSettingsViewModel.LayoutMode)));
					instanceViewModel.FolderSettings.PropertyChanged += FolderSettings_PropertyChanged;
				}
			}
		}

		private Style _LayoutOpacityIcon;
		public Style LayoutOpacityIcon
		{
			get => _LayoutOpacityIcon;
			set
			{
				if (SetProperty(ref _LayoutOpacityIcon, value))
				{
				}
			}
		}

		private PointerRoutedEventArgs? pointerRoutedEventArgs;

		public ToolbarViewModel()
		{
			RefreshClickCommand = new RelayCommand<RoutedEventArgs>(e => RefreshRequested?.Invoke(this, EventArgs.Empty));
			ViewReleaseNotesAsyncCommand = new AsyncRelayCommand(ViewReleaseNotesAsync);

			dispatcherQueue = DispatcherQueue.GetForCurrentThread();
			dragOverTimer = dispatcherQueue.CreateTimer();

			SearchBox.Escaped += SearchRegion_Escaped;
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
			UpdateService.PropertyChanged += UpdateService_OnPropertyChanged;
		}

		private async void UpdateService_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			IsUpdateAvailable = UpdateService.IsUpdateAvailable;
			IsUpdating = UpdateService.IsUpdating;

			// TODO: Bad code, result is called twice when checking for release notes
			if (UpdateService.IsReleaseNotesAvailable)
				await CheckForReleaseNotesAsync();
		}

		private async Task ViewReleaseNotesAsync()
		{
			if (ReleaseNotes is null)
				return;

			var viewModel = new ReleaseNotesDialogViewModel(ReleaseNotes);
			var dialog = _dialogService.GetDialog(viewModel);

			await dialog.TryShowAsync();
		}

		public async Task CheckForReleaseNotesAsync()
		{
			var result = await UpdateService.GetLatestReleaseNotesAsync();
			if (result is null)
				return;

			ReleaseNotes = result;
			IsReleaseNotesVisible = true;
		}

		public void RefreshWidgets()
		{
			RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
		}

		private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				// TODO: Move this to the widget page, it doesn't belong here.
				case nameof(UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget):
				case nameof(UserSettingsService.GeneralSettingsService.ShowDrivesWidget):
				case nameof(UserSettingsService.GeneralSettingsService.ShowFileTagsWidget):
				case nameof(UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget):
					RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
					OnPropertyChanged(e.SettingName);
					break;
			}
		}

		private DispatcherQueue dispatcherQueue;
		private DispatcherQueueTimer dragOverTimer;

		private ISearchBox searchBox = new SearchBoxViewModel();
		public ISearchBox SearchBox
		{
			get => searchBox;
			set => SetProperty(ref searchBox, value);
		}

		public SearchBoxViewModel SearchBoxViewModel
			=> (SearchBoxViewModel)SearchBox;

		public bool IsSingleItemOverride { get; set; } = false;

		private string? dragOverPath = null;

		public void PathBoxItem_DragLeave(object sender, DragEventArgs e)
		{
			if (((StackPanel)sender).DataContext is not PathBoxItem pathBoxItem ||
				pathBoxItem.Path == "Home")
			{
				return;
			}

			// Reset dragged over pathbox item
			if (pathBoxItem.Path == dragOverPath)
				dragOverPath = null;
		}

		private bool lockFlag = false;

		public async Task PathBoxItem_Drop(object sender, DragEventArgs e)
		{
			if (lockFlag)
				return;

			lockFlag = true;

			// Reset dragged over pathbox item
			dragOverPath = null;

			if (((StackPanel)sender).DataContext is not PathBoxItem pathBoxItem ||
				pathBoxItem.Path == "Home")
			{
				return;
			}

			var deferral = e.GetDeferral();

			var signal = new AsyncManualResetEvent();

			PathBoxItemDropped?.Invoke(this, new PathBoxItemDroppedEventArgs()
			{
				AcceptedOperation = e.AcceptedOperation,
				Package = e.DataView,
				Path = pathBoxItem.Path,
				SignalEvent = signal
			});

			await signal.WaitAsync();

			deferral.Complete();
			await Task.Yield();

			lockFlag = false;
		}

		public async Task PathBoxItem_DragOver(object sender, DragEventArgs e)
		{
			if (IsSingleItemOverride ||
				((StackPanel)sender).DataContext is not PathBoxItem pathBoxItem ||
				pathBoxItem.Path == "Home")
			{
				return;
			}

			if (dragOverPath != pathBoxItem.Path)
			{
				dragOverPath = pathBoxItem.Path;
				dragOverTimer.Stop();

				if (dragOverPath != (this as IAddressToolbar).PathComponents.LastOrDefault()?.Path)
				{
					dragOverTimer.Debounce(() =>
					{
						if (dragOverPath is not null)
						{
							dragOverTimer.Stop();
							ItemDraggedOverPathItem?.Invoke(this, new PathNavigationEventArgs()
							{
								ItemPath = dragOverPath
							});
							dragOverPath = null;
						}
					},
					TimeSpan.FromMilliseconds(1000), false);
				}
			}

			// In search page
			if (!FilesystemHelpers.HasDraggedStorageItems(e.DataView) || string.IsNullOrEmpty(pathBoxItem.Path))
			{
				e.AcceptedOperation = DataPackageOperation.None;

				return;
			}

			e.Handled = true;
			var deferral = e.GetDeferral();

			var storageItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);

			if (!storageItems.Any(storageItem =>
					!string.IsNullOrEmpty(storageItem?.Path) &&
					storageItem.Path.Replace(pathBoxItem.Path, string.Empty, StringComparison.Ordinal)
						.Trim(Path.DirectorySeparatorChar)
						.Contains(Path.DirectorySeparatorChar)))
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}

			// Copy be default when dragging from zip
			else if (storageItems.Any(x =>
					x.Item is ZipStorageFile ||
					x.Item is ZipStorageFolder) ||
					ZipStorageFolder.IsZipPath(pathBoxItem.Path))
			{
				e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), pathBoxItem.Title);
				e.AcceptedOperation = DataPackageOperation.Copy;
			}
			else
			{
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), pathBoxItem.Title);
				e.AcceptedOperation = DataPackageOperation.Move;
			}

			deferral.Complete();
		}

		public bool IsEditModeEnabled
		{
			get => ManualEntryBoxLoaded;
			set
			{
				if (value)
				{
					EditModeEnabled?.Invoke(this, EventArgs.Empty);

					var visiblePath = AddressToolbar?.FindDescendant<AutoSuggestBox>(x => x.Name == "VisiblePath");
					visiblePath?.Focus(FocusState.Programmatic);
					visiblePath?.FindDescendant<TextBox>()?.SelectAll();

					AddressBarTextEntered?.Invoke(this, new AddressBarTextEnteredEventArgs() { AddressBarTextField = visiblePath });
				}
				else
				{
					IsCommandPaletteOpen = false;
					ManualEntryBoxLoaded = false;
					ClickablePathLoaded = true;
				}
			}
		}

		private bool manualEntryBoxLoaded;
		public bool ManualEntryBoxLoaded
		{
			get => manualEntryBoxLoaded;
			set => SetProperty(ref manualEntryBoxLoaded, value);
		}

		private bool clickablePathLoaded = true;
		public bool ClickablePathLoaded
		{
			get => clickablePathLoaded;
			set => SetProperty(ref clickablePathLoaded, value);
		}

		private string pathControlDisplayText;
		public string PathControlDisplayText
		{
			get => pathControlDisplayText;
			set => SetProperty(ref pathControlDisplayText, value);
		}

		public ICommand RefreshClickCommand { get; }
		public ICommand ViewReleaseNotesAsyncCommand { get; }

		public void PathItemSeparator_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var pathSeparatorIcon = sender as FontIcon;
			if (pathSeparatorIcon is null || pathSeparatorIcon.DataContext is null)
				return;

			ToolbarPathItemLoaded?.Invoke(pathSeparatorIcon, new ToolbarPathItemLoadedEventArgs()
			{
				Item = (PathBoxItem)pathSeparatorIcon.DataContext,
				OpenedFlyout = (MenuFlyout)pathSeparatorIcon.ContextFlyout
			});
		}

		public void PathboxItemFlyout_Opened(object sender, object e)
		{
			ToolbarFlyoutOpened?.Invoke(this, new ToolbarFlyoutOpenedEventArgs() { OpenedFlyout = (MenuFlyout)sender });
		}

		public void VisiblePath_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				AddressBarTextEntered?.Invoke(this, new AddressBarTextEnteredEventArgs() { AddressBarTextField = sender });
		}

		public void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			PathBoxQuerySubmitted?.Invoke(this, new ToolbarQuerySubmittedEventArgs() { QueryText = args.QueryText });

			(this as IAddressToolbar).IsEditModeEnabled = false;
		}

		public void PathBoxItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Mouse)
				return;

			var ptrPt = e.GetCurrentPoint(AddressToolbar);
			pointerRoutedEventArgs = ptrPt.Properties.IsMiddleButtonPressed ? e : null;
		}

		public async Task PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var itemTappedPath = ((sender as TextBlock)?.DataContext as PathBoxItem)?.Path;
			if (itemTappedPath is null)
				return;

			if (pointerRoutedEventArgs is not null)
			{
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), itemTappedPath);
				}, DispatcherQueuePriority.Low);
				e.Handled = true;
				pointerRoutedEventArgs = null;

				return;
			}

			ToolbarPathItemInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = itemTappedPath
			});
		}

		public void OpenCommandPalette()
		{
			PathText = ">";
			IsCommandPaletteOpen = true;
			ManualEntryBoxLoaded = true;
			ClickablePathLoaded = false;

			var visiblePath = AddressToolbar?.FindDescendant<AutoSuggestBox>(x => x.Name == "VisiblePath");
			AddressBarTextEntered?.Invoke(this, new AddressBarTextEnteredEventArgs() { AddressBarTextField = visiblePath });
		}

		public void SwitchSearchBoxVisibility()
		{
			if (IsSearchBoxVisible)
			{
				CloseSearchBox(true);
			}
			else
			{
				IsSearchBoxVisible = true;

				// Given that binding and layouting might take a few cycles, when calling UpdateLayout
				// we can guarantee that the focus call will be able to find an open ASB
				var searchbox = AddressToolbar?.FindDescendant("SearchRegion") as SearchBox;
				searchbox?.UpdateLayout();
				searchbox?.Focus(FocusState.Programmatic);
			}
		}

		public void UpdateAdditionalActions()
		{
			OnPropertyChanged(nameof(HasAdditionalAction));
		}

		private AddressToolbar? AddressToolbar => (MainWindow.Instance.Content as Frame)?.FindDescendant<AddressToolbar>();

		private void CloseSearchBox(bool doFocus = false)
		{
			if (searchBox.WasQuerySubmitted)
			{
				searchBox.WasQuerySubmitted = false;
			}
			else
			{
				SearchBox.Query = string.Empty;
				IsSearchBoxVisible = false;

				if (doFocus)
				{
					var page = Ioc.Default.GetRequiredService<IContentPageContext>().ShellPage?.SlimContentPage;

					if (page is BaseGroupableLayoutPage svb && svb.IsLoaded)
						page.ItemManipulationModel.FocusFileList();
					else
						AddressToolbar?.Focus(FocusState.Programmatic);
				}
			}
		}

		public bool SearchHasFocus { get; private set; }

		public void SearchRegion_GotFocus(object sender, RoutedEventArgs e)
		{
			SearchHasFocus = true;
		}

		public void SearchRegion_LostFocus(object sender, RoutedEventArgs e)
		{
			var element = FocusManager.GetFocusedElement();
			if (element is FlyoutBase or AppBarButton)
				return;

			SearchHasFocus = false;
			CloseSearchBox();
		}

		private void SearchRegion_Escaped(object? sender, ISearchBox searchBox)
			=> CloseSearchBox(true);

		public IAsyncRelayCommand? OpenNewWindowCommand { get; set; }

		public ICommand? CreateNewFileCommand { get; set; }

		public ICommand? Share { get; set; }

		public ICommand? UpdateCommand { get; set; }

		public async Task SetPathBoxDropDownFlyoutAsync(MenuFlyout flyout, PathBoxItem pathItem, IShellPage shellPage)
		{
			var nextPathItemTitle = PathComponents[PathComponents.IndexOf(pathItem) + 1].Title;
			IList<StorageFolderWithPath>? childFolders = null;

			StorageFolderWithPath folder = await shellPage.FilesystemViewModel.GetFolderWithPathFromPathAsync(pathItem.Path);
			if (folder is not null)
				childFolders = (await FilesystemTasks.Wrap(() => folder.GetFoldersWithPathAsync(string.Empty))).Result;

			flyout.Items?.Clear();

			if (childFolders is null || childFolders.Count == 0)
			{
				var flyoutItem = new MenuFlyoutItem
				{
					Icon = new FontIcon { Glyph = "\uE7BA" },
					Text = "SubDirectoryAccessDenied".GetLocalizedResource(),
					//Foreground = (SolidColorBrush)Application.Current.Resources["SystemControlErrorTextForegroundBrush"],
					FontSize = 12
				};

				flyout.Items?.Add(flyoutItem);

				return;
			}

			var boldFontWeight = new FontWeight { Weight = 800 };
			var normalFontWeight = new FontWeight { Weight = 400 };

			var workingPath =
				PathComponents[PathComponents.Count - 1].Path?.TrimEnd(Path.DirectorySeparatorChar);

			foreach (var childFolder in childFolders)
			{
				var isPathItemFocused = childFolder.Item.Name == nextPathItemTitle;

				var flyoutItem = new MenuFlyoutItem
				{
					Icon = new FontIcon
					{
						Glyph = "\uED25",
						FontWeight = isPathItemFocused ? boldFontWeight : normalFontWeight
					},
					Text = childFolder.Item.Name,
					FontSize = 12,
					FontWeight = isPathItemFocused ? boldFontWeight : normalFontWeight
				};

				if (workingPath != childFolder.Path)
				{
					flyoutItem.Click += (sender, args) =>
					{
						// Navigate to the directory
						shellPage.NavigateToPath(childFolder.Path);
					};
				}

				flyout.Items?.Add(flyoutItem);
			}
		}

		public async Task CheckPathInputAsync(string currentInput, string currentSelectedPath, IShellPage shellPage)
		{
			if (currentInput.StartsWith('>'))
			{
				var code = currentInput.Substring(1).Trim();
				var command = Commands[code];

				if (command == Commands.None)
					await DialogDisplayHelper.ShowDialogAsync("InvalidCommand".GetLocalizedResource(),
						string.Format("InvalidCommandContent".GetLocalizedResource(), code));
				else if (!command.IsExecutable)
					await DialogDisplayHelper.ShowDialogAsync("CommandNotExecutable".GetLocalizedResource(),
						string.Format("CommandNotExecutableContent".GetLocalizedResource(), command.Code));
				else
					await command.ExecuteAsync();

				return;
			}

			var isFtp = FtpHelpers.IsFtpPath(currentInput);

			if (currentInput.Contains('/') && !isFtp)
				currentInput = currentInput.Replace("/", "\\", StringComparison.Ordinal);

			currentInput = currentInput.Replace("\\\\", "\\", StringComparison.Ordinal);

			if (currentInput.StartsWith('\\') && !currentInput.StartsWith("\\\\", StringComparison.Ordinal))
				currentInput = currentInput.Insert(0, "\\");

			if (currentSelectedPath == currentInput || string.IsNullOrWhiteSpace(currentInput))
				return;

			if (currentInput != shellPage.FilesystemViewModel.WorkingDirectory || shellPage.CurrentPageType == typeof(HomePage))
			{
				if (currentInput.Equals("Home", StringComparison.OrdinalIgnoreCase) || currentInput.Equals("Home".GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
				{
					shellPage.NavigateHome();
				}
				else
				{
					currentInput = StorageFileExtensions.GetResolvedPath(currentInput, isFtp);
					if (currentSelectedPath == currentInput)
						return;

					var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(currentInput));

					var resFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(currentInput, item));
					if (resFolder || FolderHelpers.CheckFolderAccessWithWin32(currentInput))
					{
						var matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x => PathNormalization.NormalizePath(currentInput).StartsWith(PathNormalization.NormalizePath(x.Path), StringComparison.Ordinal));
						if (matchingDrive is not null && matchingDrive.Type == Data.Items.DriveType.CDRom && matchingDrive.MaxSpace == ByteSizeLib.ByteSize.FromBytes(0))
						{
							bool ejectButton = await DialogDisplayHelper.ShowDialogAsync("InsertDiscDialog/Title".GetLocalizedResource(), string.Format("InsertDiscDialog/Text".GetLocalizedResource(), matchingDrive.Path), "InsertDiscDialog/OpenDriveButton".GetLocalizedResource(), "Close".GetLocalizedResource());
							if (ejectButton)
							{
								var result = await DriveHelpers.EjectDeviceAsync(matchingDrive.Path);
								await UIHelpers.ShowDeviceEjectResultAsync(matchingDrive.Type, result);
							}
							return;
						}
						var pathToNavigate = resFolder.Result?.Path ?? currentInput;
						shellPage.NavigateToPath(pathToNavigate);
					}
					else if (isFtp)
					{
						shellPage.NavigateToPath(currentInput);
					}
					else // Not a folder or inaccessible
					{
						var resFile = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(currentInput, item));
						if (resFile)
						{
							var pathToInvoke = resFile.Result.Path;
							await Win32Helpers.InvokeWin32ComponentAsync(pathToInvoke, shellPage);
						}
						else // Not a file or not accessible
						{
							var workingDir =
								string.IsNullOrEmpty(shellPage.FilesystemViewModel.WorkingDirectory) ||
								shellPage.CurrentPageType == typeof(HomePage) ?
									Constants.UserEnvironmentPaths.HomePath :
									shellPage.FilesystemViewModel.WorkingDirectory;

							if (await LaunchApplicationFromPath(currentInput, workingDir))
								return;

							try
							{
								if (!await Windows.System.Launcher.LaunchUriAsync(new Uri(currentInput)))
									await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalizedResource(),
										string.Format("InvalidItemDialogContent".GetLocalizedResource(), Environment.NewLine, resFolder.ErrorCode.ToString()));
							}
							catch (Exception ex) when (ex is UriFormatException || ex is ArgumentException)
							{
								await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalizedResource(),
									string.Format("InvalidItemDialogContent".GetLocalizedResource(), Environment.NewLine, resFolder.ErrorCode.ToString()));
							}
						}
					}
				}

				PathControlDisplayText = shellPage.FilesystemViewModel.WorkingDirectory;
			}
		}

		private static async Task<bool> LaunchApplicationFromPath(string currentInput, string workingDir)
		{
			var trimmedInput = currentInput.Trim();
			var fileName = trimmedInput;
			var arguments = string.Empty;
			if (trimmedInput.Contains(' '))
			{
				var positionOfBlank = trimmedInput.IndexOf(' ');
				fileName = trimmedInput.Substring(0, positionOfBlank);
				arguments = currentInput.Substring(currentInput.IndexOf(' '));
			}

			return await LaunchHelper.LaunchAppAsync(fileName, arguments, workingDir);
		}

		public async Task SetAddressBarSuggestionsAsync(AutoSuggestBox sender, IShellPage shellpage, int maxSuggestions = 7)
		{
			if (!string.IsNullOrWhiteSpace(sender.Text) && shellpage.FilesystemViewModel is not null)
			{
				if (!await SafetyExtensions.IgnoreExceptions(async () =>
				{
					IList<NavigationBarSuggestionItem>? suggestions = null;

					if (sender.Text.StartsWith(">"))
					{
						IsCommandPaletteOpen = true;
						var searchText = sender.Text.Substring(1).Trim();
						suggestions = Commands.Where(command => command.IsExecutable &&
							(command.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
							|| command.Code.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)))
						.Select(command => new NavigationBarSuggestionItem()
						{
							Text = ">" + command.Code,
							PrimaryDisplay = command.Description,
							SupplementaryDisplay = command.HotKeyText,
							SearchText = searchText,
						}).ToList();
					}
					else
					{
						IsCommandPaletteOpen = false;
						var isFtp = FtpHelpers.IsFtpPath(sender.Text);
						var expandedPath = StorageFileExtensions.GetResolvedPath(sender.Text, isFtp);
						var folderPath = PathNormalization.GetParentDir(expandedPath) ?? expandedPath;
						StorageFolderWithPath folder = await shellpage.FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);

						if (folder is null)
							return false;

						var currPath = await folder.GetFoldersWithPathAsync(Path.GetFileName(expandedPath), (uint)maxSuggestions);
						if (currPath.Count >= maxSuggestions)
						{
							suggestions = currPath.Select(x => new NavigationBarSuggestionItem()
							{
								Text = x.Path,
								PrimaryDisplay = x.Item.DisplayName
							}).ToList();
						}
						else if (currPath.Any())
						{
							var subPath = await currPath.First().GetFoldersWithPathAsync((uint)(maxSuggestions - currPath.Count));
							suggestions = currPath.Select(x => new NavigationBarSuggestionItem()
							{
								Text = x.Path,
								PrimaryDisplay = x.Item.DisplayName
							}).Concat(
								subPath.Select(x => new NavigationBarSuggestionItem()
								{
									Text = x.Path,
									PrimaryDisplay = PathNormalization.Combine(currPath.First().Item.DisplayName, x.Item.DisplayName)
								})).ToList();
						}
					}

					if (suggestions is null || suggestions.Count == 0)
					{
						suggestions = new List<NavigationBarSuggestionItem>() { new NavigationBarSuggestionItem() {
						Text = shellpage.FilesystemViewModel.WorkingDirectory,
						PrimaryDisplay = "NavigationToolbarVisiblePathNoResults".GetLocalizedResource() } };
					}

					// NavigationBarSuggestions becoming empty causes flickering of the suggestion box
					// Here we check whether at least an element is in common between old and new list
					if (!NavigationBarSuggestions.IntersectBy(suggestions, x => x.PrimaryDisplay).Any())
					{
						// No elements in common, update the list in-place
						for (int index = 0; index < suggestions.Count; index++)
						{
							if (index < NavigationBarSuggestions.Count)
							{
								NavigationBarSuggestions[index].Text = suggestions[index].Text;
								NavigationBarSuggestions[index].PrimaryDisplay = suggestions[index].PrimaryDisplay;
								NavigationBarSuggestions[index].SecondaryDisplay = suggestions[index].SecondaryDisplay;
								NavigationBarSuggestions[index].SupplementaryDisplay = suggestions[index].SupplementaryDisplay;
								NavigationBarSuggestions[index].SearchText = suggestions[index].SearchText;
							}
							else
							{
								NavigationBarSuggestions.Add(suggestions[index]);
							}
						}

						while (NavigationBarSuggestions.Count > suggestions.Count)
							NavigationBarSuggestions.RemoveAt(NavigationBarSuggestions.Count - 1);
					}
					else
					{
						// At least an element in common, show animation
						foreach (var s in NavigationBarSuggestions.ExceptBy(suggestions, x => x.PrimaryDisplay).ToList())
							NavigationBarSuggestions.Remove(s);

						for (int index = 0; index < suggestions.Count; index++)
						{
							if (NavigationBarSuggestions.Count > index && NavigationBarSuggestions[index].PrimaryDisplay == suggestions[index].PrimaryDisplay)
								NavigationBarSuggestions[index].SearchText = suggestions[index].SearchText;
							else
								NavigationBarSuggestions.Insert(index, suggestions[index]);
						}
					}

					return true;
				}))
				{
					NavigationBarSuggestions.Clear();
					NavigationBarSuggestions.Add(new NavigationBarSuggestionItem()
					{
						Text = shellpage.FilesystemViewModel.WorkingDirectory,
						PrimaryDisplay = "NavigationToolbarVisiblePathNoResults".GetLocalizedResource()
					});
				}
			}
		}

		private void FolderSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(FolderSettingsViewModel.GridViewSize):
				case nameof(FolderSettingsViewModel.LayoutMode):
					LayoutOpacityIcon = instanceViewModel.FolderSettings.LayoutMode switch
					{
						FolderLayoutModes.TilesView => Commands.LayoutTiles.OpacityStyle!,
						FolderLayoutModes.ColumnView => Commands.LayoutColumns.OpacityStyle!,
						FolderLayoutModes.GridView =>
							instanceViewModel.FolderSettings.GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeSmall
								? Commands.LayoutGridSmall.OpacityStyle!
								: instanceViewModel.FolderSettings.GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeMedium
									? Commands.LayoutGridMedium.OpacityStyle!
									: Commands.LayoutGridLarge.OpacityStyle!,
						_ => Commands.LayoutDetails.OpacityStyle!
					};
					OnPropertyChanged(nameof(IsTilesLayout));
					OnPropertyChanged(nameof(IsColumnLayout));
					OnPropertyChanged(nameof(IsGridSmallLayout));
					OnPropertyChanged(nameof(IsGridMediumLayout));
					OnPropertyChanged(nameof(IsGridLargeLayout));
					OnPropertyChanged(nameof(IsDetailsLayout));
					break;
			}
		}

		private bool hasItem = false;
		public bool HasItem
		{
			get => hasItem;
			set => SetProperty(ref hasItem, value);
		}

		private List<ListedItem>? selectedItems;

		public List<ListedItem> SelectedItems
		{
			get => selectedItems;
			set
			{
				if (SetProperty(ref selectedItems, value))
				{
					OnPropertyChanged(nameof(CanCopy));
					OnPropertyChanged(nameof(CanExtract));
					OnPropertyChanged(nameof(ExtractToText));
					OnPropertyChanged(nameof(IsArchiveOpened));
					OnPropertyChanged(nameof(IsSelectionArchivesOnly));
					OnPropertyChanged(nameof(IsMultipleArchivesSelected));
					OnPropertyChanged(nameof(IsInfFile));
					OnPropertyChanged(nameof(IsPowerShellScript));
					OnPropertyChanged(nameof(IsImage));
					OnPropertyChanged(nameof(IsMultipleImageSelected));
					OnPropertyChanged(nameof(IsFont));
					OnPropertyChanged(nameof(HasAdditionalAction));
				}
			}
		}

		public bool HasAdditionalAction => InstanceViewModel.IsPageTypeRecycleBin || IsPowerShellScript || CanExtract || IsImage || IsFont || IsInfFile;
		public bool CanCopy => SelectedItems is not null && SelectedItems.Any();
		public bool CanExtract => IsArchiveOpened ? (SelectedItems is null || !SelectedItems.Any()) : IsSelectionArchivesOnly;
		public bool IsArchiveOpened => FileExtensionHelpers.IsZipFile(Path.GetExtension(pathControlDisplayText));
		public bool IsSelectionArchivesOnly => SelectedItems is not null && SelectedItems.Any() && SelectedItems.All(x => FileExtensionHelpers.IsZipFile(x.FileExtension)) && !InstanceViewModel.IsPageTypeRecycleBin;
		public bool IsMultipleArchivesSelected => IsSelectionArchivesOnly && SelectedItems.Count > 1;
		public bool IsPowerShellScript => SelectedItems is not null && SelectedItems.Count == 1 && FileExtensionHelpers.IsPowerShellFile(SelectedItems.First().FileExtension) && !InstanceViewModel.IsPageTypeRecycleBin;
		public bool IsImage => SelectedItems is not null && SelectedItems.Any() && SelectedItems.All(x => FileExtensionHelpers.IsImageFile(x.FileExtension)) && !InstanceViewModel.IsPageTypeRecycleBin;
		public bool IsMultipleImageSelected => SelectedItems is not null && SelectedItems.Count > 1 && SelectedItems.All(x => FileExtensionHelpers.IsImageFile(x.FileExtension)) && !InstanceViewModel.IsPageTypeRecycleBin;
		public bool IsInfFile => SelectedItems is not null && SelectedItems.Count == 1 && FileExtensionHelpers.IsInfFile(SelectedItems.First().FileExtension) && !InstanceViewModel.IsPageTypeRecycleBin;
		public bool IsFont => SelectedItems is not null && SelectedItems.Any() && SelectedItems.All(x => FileExtensionHelpers.IsFontFile(x.FileExtension)) && !InstanceViewModel.IsPageTypeRecycleBin;

		public bool IsTilesLayout => instanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.TilesView;
		public bool IsColumnLayout => instanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.ColumnView;
		public bool IsGridSmallLayout => instanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.GridView && instanceViewModel.FolderSettings.GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeSmall;
		public bool IsGridMediumLayout => instanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.GridView && !IsGridSmallLayout && instanceViewModel.FolderSettings.GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeMedium;
		public bool IsGridLargeLayout => instanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.GridView && !IsGridSmallLayout && !IsGridMediumLayout;
		public bool IsDetailsLayout => !IsTilesLayout && !IsColumnLayout && !IsGridSmallLayout && !IsGridMediumLayout && !IsGridLargeLayout;

		public string ExtractToText
			=> IsSelectionArchivesOnly ? SelectedItems.Count > 1 ? string.Format("ExtractToChildFolder".GetLocalizedResource(), $"*{Path.DirectorySeparatorChar}") : string.Format("ExtractToChildFolder".GetLocalizedResource() + "\\", Path.GetFileNameWithoutExtension(selectedItems.First().Name)) : "ExtractToChildFolder".GetLocalizedResource();

		public void Dispose()
		{
			SearchBox.Escaped -= SearchRegion_Escaped;
			UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
		}
	}
}
