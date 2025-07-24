// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using System.Windows.Input;
using Windows.AI.Actions;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Text;

namespace Files.App.ViewModels.UserControls
{
	public sealed partial class NavigationToolbarViewModel : ObservableObject, IAddressToolbarViewModel, IDisposable
	{
		// Constants

		private const int MaxSuggestionsCount = 10;

		public const string OmnibarPathModeName = "OmnibarPathMode";
		public const string OmnibarPaletteModeName = "OmnibarCommandPaletteMode";
		public const string OmnibarSearchModeName = "OmnibarSearchMode";

		// Dependency injections

		private readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IAppearanceSettingsService AppearanceSettingsService = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private readonly IUpdateService UpdateService = Ioc.Default.GetRequiredService<IUpdateService>();
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly StatusCenterViewModel OngoingTasksViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		// Fields

		private readonly DispatcherQueue _dispatcherQueue;
		private readonly DispatcherQueueTimer _dragOverTimer;

		private string? _dragOverPath;
		private bool _lockFlag;
		private PointerRoutedEventArgs? _pointerRoutedEventArgs;

		// Events

		public delegate void ToolbarPathItemInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public delegate void PathBoxItemDroppedEventHandler(object sender, PathBoxItemDroppedEventArgs e);
		public event ToolbarPathItemInvokedEventHandler? ToolbarPathItemInvoked;
		public event IAddressToolbarViewModel.ItemDraggedOverPathItemEventHandler? ItemDraggedOverPathItem;
		public event IAddressToolbarViewModel.ToolbarQuerySubmittedEventHandler? PathBoxQuerySubmitted;

		public event PathBoxItemDroppedEventHandler? PathBoxItemDropped;
		public event EventHandler? RefreshWidgetsRequested;

		// Properties

		internal static ActionRuntime? ActionRuntime { get; private set; }

		public ObservableCollection<PathBoxItem> PathComponents { get; } = [];

		public ObservableCollection<NavigationBarSuggestionItem> NavigationBarSuggestions { get; } = [];

		internal ObservableCollection<OmnibarPathModeSuggestionModel> PathModeSuggestionItems { get; } = [];

		internal ObservableCollection<NavigationBarSuggestionItem> OmnibarCommandPaletteModeSuggestionItems { get; } = [];

		internal ObservableCollection<SuggestionModel> OmnibarSearchModeSuggestionItems { get; } = [];

		public bool IsSingleItemOverride { get; set; }

		public bool ShowStatusCenterButton =>
			AppearanceSettingsService.StatusCenterVisibility == StatusCenterVisibility.Always ||
			(AppearanceSettingsService.StatusCenterVisibility == StatusCenterVisibility.DuringOngoingFileOperations && OngoingTasksViewModel.HasAnyItem);

		public bool ShowShelfPaneToggleButton => AppearanceSettingsService.ShowShelfPaneToggleButton && AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev;

		private NavigationToolbar? AddressToolbar => (MainWindow.Instance.Content as Frame)?.FindDescendant<NavigationToolbar>();

		public bool HasAdditionalAction =>
			InstanceViewModel.IsPageTypeRecycleBin ||
			Commands.RunWithPowershell.IsExecutable ||
			CanExtract ||
			Commands.DecompressArchive.IsExecutable ||
			Commands.DecompressArchiveHere.IsExecutable ||
			Commands.DecompressArchiveHereSmart.IsExecutable ||
			Commands.DecompressArchiveToChildFolder.IsExecutable ||
			Commands.EditInNotepad.IsExecutable ||
			Commands.RotateLeft.IsExecutable ||
			Commands.RotateRight.IsExecutable ||
			Commands.SetAsAppBackground.IsExecutable ||
			Commands.SetAsWallpaperBackground.IsExecutable ||
			Commands.SetAsLockscreenBackground.IsExecutable ||
			Commands.SetAsSlideshowBackground.IsExecutable ||
			Commands.InstallFont.IsExecutable ||
			Commands.InstallInfDriver.IsExecutable ||
			Commands.InstallCertificate.IsExecutable;

		public bool CanExtract => Commands.DecompressArchive.CanExecute(null) || Commands.DecompressArchiveHere.CanExecute(null) || Commands.DecompressArchiveHereSmart.CanExecute(null) || Commands.DecompressArchiveToChildFolder.CanExecute(null);

		public bool IsCardsLayout => _InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.CardsView;
		public bool IsColumnLayout => _InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.ColumnView;
		public bool IsGridLayout => _InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.GridView;
		public bool IsDetailsLayout => _InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.DetailsView;
		public bool IsListLayout => _InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.ListView;

		public bool IsLayoutSizeCompact =>
			(IsDetailsLayout && UserSettingsService.LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.Compact) ||
			(IsListLayout && UserSettingsService.LayoutSettingsService.ListViewSize == ListViewSizeKind.Compact) ||
			(IsColumnLayout && UserSettingsService.LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.Compact);

		public bool IsLayoutSizeSmall =>
			(IsDetailsLayout && UserSettingsService.LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.Small) ||
			(IsListLayout && UserSettingsService.LayoutSettingsService.ListViewSize == ListViewSizeKind.Small) ||
			(IsColumnLayout && UserSettingsService.LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.Small) ||
			(IsCardsLayout && UserSettingsService.LayoutSettingsService.CardsViewSize == CardsViewSizeKind.Small) ||
			(IsGridLayout && UserSettingsService.LayoutSettingsService.GridViewSize == GridViewSizeKind.Small);

		public bool IsLayoutSizeMedium =>
			(IsDetailsLayout && UserSettingsService.LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.Medium) ||
			(IsListLayout && UserSettingsService.LayoutSettingsService.ListViewSize == ListViewSizeKind.Medium) ||
			(IsColumnLayout && UserSettingsService.LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.Medium) ||
			(IsCardsLayout && UserSettingsService.LayoutSettingsService.CardsViewSize == CardsViewSizeKind.Medium) ||
			(IsGridLayout && UserSettingsService.LayoutSettingsService.GridViewSize == GridViewSizeKind.Medium);

		public bool IsLayoutSizeLarge =>
			(IsDetailsLayout && UserSettingsService.LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.Large) ||
			(IsListLayout && UserSettingsService.LayoutSettingsService.ListViewSize == ListViewSizeKind.Large) ||
			(IsColumnLayout && UserSettingsService.LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.Large) ||
			(IsCardsLayout && UserSettingsService.LayoutSettingsService.CardsViewSize == CardsViewSizeKind.Large) ||
			(IsGridLayout && UserSettingsService.LayoutSettingsService.GridViewSize == GridViewSizeKind.Large);

		public bool IsLayoutSizeExtraLarge =>
			(IsDetailsLayout && UserSettingsService.LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.ExtraLarge) ||
			(IsListLayout && UserSettingsService.LayoutSettingsService.ListViewSize == ListViewSizeKind.ExtraLarge) ||
			(IsColumnLayout && UserSettingsService.LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.ExtraLarge) ||
			(IsCardsLayout && UserSettingsService.LayoutSettingsService.CardsViewSize == CardsViewSizeKind.ExtraLarge) ||
			(IsGridLayout && UserSettingsService.LayoutSettingsService.GridViewSize == GridViewSizeKind.ExtraLarge);

		private bool _IsDynamicOverflowEnabled;
		public bool IsDynamicOverflowEnabled { get => _IsDynamicOverflowEnabled; set => SetProperty(ref _IsDynamicOverflowEnabled, value); }

		private bool _IsUpdating;
		public bool IsUpdating { get => _IsUpdating; set => SetProperty(ref _IsUpdating, value); }

		private bool _IsUpdateAvailable;
		public bool IsUpdateAvailable { get => _IsUpdateAvailable; set => SetProperty(ref _IsUpdateAvailable, value); }

		private bool _CanCopyPathInPage;
		public bool CanCopyPathInPage { get => _CanCopyPathInPage; set => SetProperty(ref _CanCopyPathInPage, value); }

		private bool _CanGoBack;
		public bool CanGoBack { get => _CanGoBack; set => SetProperty(ref _CanGoBack, value); }

		private bool _CanGoForward;
		public bool CanGoForward { get => _CanGoForward; set => SetProperty(ref _CanGoForward, value); }

		private bool _CanNavigateToParent;
		public bool CanNavigateToParent { get => _CanNavigateToParent; set => SetProperty(ref _CanNavigateToParent, value); }

		private bool _PreviewPaneEnabled;
		public bool PreviewPaneEnabled { get => _PreviewPaneEnabled; set => SetProperty(ref _PreviewPaneEnabled, value); }

		private bool _CanRefresh;
		public bool CanRefresh { get => _CanRefresh; set => SetProperty(ref _CanRefresh, value); }

		private string _PathControlDisplayText;
		[Obsolete("Superseded by Omnibar.")]
		public string PathControlDisplayText { get => _PathControlDisplayText; set => SetProperty(ref _PathControlDisplayText, value); }

		private bool _HasItem = false;
		public bool HasItem { get => _HasItem; set => SetProperty(ref _HasItem, value); }

		private Style _LayoutThemedIcon;
		public Style LayoutThemedIcon { get => _LayoutThemedIcon; set => SetProperty(ref _LayoutThemedIcon, value); }

		// SetProperty doesn't seem to properly notify the binding in path bar
		private string? _PathText;
		public string? PathText
		{
			get => _PathText;
			set
			{
				_PathText = value;
				OnPropertyChanged(nameof(PathText));
			}
		}

		// Workaround to ensure Omnibar is only loaded after the ViewModel is initialized
		public bool LoadOmnibar =>
			true;

		private string? _OmnibarCommandPaletteModeText;
		public string? OmnibarCommandPaletteModeText { get => _OmnibarCommandPaletteModeText; set => SetProperty(ref _OmnibarCommandPaletteModeText, value); }

		private string? _OmnibarSearchModeText;
		public string? OmnibarSearchModeText { get => _OmnibarSearchModeText; set => SetProperty(ref _OmnibarSearchModeText, value); }

		private string _OmnibarCurrentSelectedModeName = OmnibarPathModeName;
		public string OmnibarCurrentSelectedModeName { get => _OmnibarCurrentSelectedModeName; set => SetProperty(ref _OmnibarCurrentSelectedModeName, value); }

		private CurrentInstanceViewModel _InstanceViewModel;
		public CurrentInstanceViewModel InstanceViewModel
		{
			get => _InstanceViewModel;
			set
			{
				if (_InstanceViewModel?.FolderSettings is not null)
					_InstanceViewModel.FolderSettings.PropertyChanged -= FolderSettings_PropertyChanged;

				if (SetProperty(ref _InstanceViewModel, value) && _InstanceViewModel?.FolderSettings is not null)
				{
					FolderSettings_PropertyChanged(this, new PropertyChangedEventArgs(nameof(LayoutPreferencesManager.LayoutMode)));
					_InstanceViewModel.FolderSettings.PropertyChanged += FolderSettings_PropertyChanged;
				}
			}
		}

		private List<ListedItem>? _SelectedItems;
		public List<ListedItem>? SelectedItems
		{
			get => _SelectedItems;
			set
			{
				if (SetProperty(ref _SelectedItems, value))
				{
					OnPropertyChanged(nameof(CanExtract));
					OnPropertyChanged(nameof(HasAdditionalAction));

					// Workaround to ensure the overflow button is only displayed when there are overflow items
					IsDynamicOverflowEnabled = false;
					IsDynamicOverflowEnabled = true;
				}
			}
		}

		// Commands

		public IAsyncRelayCommand? OpenNewWindowCommand { get; set; }
		public ICommand? CreateNewFileCommand { get; set; }
		public ICommand? Share { get; set; }
		public ICommand? UpdateCommand { get; set; }

		// Constructor

		public NavigationToolbarViewModel()
		{
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
			_dragOverTimer = _dispatcherQueue.CreateTimer();

			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
			UpdateService.PropertyChanged += UpdateService_OnPropertyChanged;

			Commands.DecompressArchive.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName is nameof(Commands.DecompressArchive.IsExecutable))
					OnPropertyChanged(nameof(CanExtract));
			};

			Commands.DecompressArchiveHere.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName is nameof(Commands.DecompressArchiveHere.IsExecutable))
					OnPropertyChanged(nameof(CanExtract));
			};

			Commands.DecompressArchiveHereSmart.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName is nameof(Commands.DecompressArchiveHereSmart.IsExecutable))
					OnPropertyChanged(nameof(CanExtract));
			};

			Commands.DecompressArchiveHereSmart.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName is nameof(Commands.DecompressArchiveToChildFolder.IsExecutable))
					OnPropertyChanged(nameof(CanExtract));
			};

			AppearanceSettingsService.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(AppearanceSettingsService.StatusCenterVisibility):
						OnPropertyChanged(nameof(ShowStatusCenterButton));
						break;
					case nameof(AppearanceSettingsService.ShowShelfPaneToggleButton):
						OnPropertyChanged(nameof(ShowShelfPaneToggleButton));
						break;
				}
			};
			OngoingTasksViewModel.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(OngoingTasksViewModel.HasAnyItem):
						OnPropertyChanged(nameof(ShowStatusCenterButton));
						break;
				}
			};
		}

		// Methods

		private void UpdateService_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			IsUpdateAvailable = UpdateService.IsUpdateAvailable;
			IsUpdating = UpdateService.IsUpdating;
		}

		private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				// TODO: Move this to the widget page, it doesn't belong here.
				case nameof(UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget):
				case nameof(UserSettingsService.GeneralSettingsService.ShowDrivesWidget):
				case nameof(UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget):
				case nameof(UserSettingsService.GeneralSettingsService.ShowFileTagsWidget):
				case nameof(UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget):
					RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
					OnPropertyChanged(e.SettingName);
					break;
				case nameof(UserSettingsService.LayoutSettingsService.DetailsViewSize):
				case nameof(UserSettingsService.LayoutSettingsService.ListViewSize):
				case nameof(UserSettingsService.LayoutSettingsService.ColumnsViewSize):
				case nameof(UserSettingsService.LayoutSettingsService.CardsViewSize):
				case nameof(UserSettingsService.LayoutSettingsService.GridViewSize):
					OnPropertyChanged(nameof(IsLayoutSizeCompact));
					OnPropertyChanged(nameof(IsLayoutSizeSmall));
					OnPropertyChanged(nameof(IsLayoutSizeMedium));
					OnPropertyChanged(nameof(IsLayoutSizeLarge));
					OnPropertyChanged(nameof(IsLayoutSizeExtraLarge));
					break;
			}
		}

		[Obsolete("Superseded by Omnibar.")]
		public void PathBoxItem_DragLeave(object sender, DragEventArgs e)
		{
			if (((FrameworkElement)sender).DataContext is not PathBoxItem pathBoxItem ||
				pathBoxItem.Path == "Home" ||
				pathBoxItem.Path == "ReleaseNotes" ||
				pathBoxItem.Path == "Settings")
			{
				return;
			}

			// Reset dragged over pathbox item
			if (pathBoxItem.Path == _dragOverPath)
				_dragOverPath = null;
		}

		[Obsolete("Superseded by Omnibar.")]
		public async Task PathBoxItem_Drop(object sender, DragEventArgs e)
		{
			if (_lockFlag)
				return;

			_lockFlag = true;

			// Reset dragged over pathbox item
			_dragOverPath = null;

			if (((FrameworkElement)sender).DataContext is not PathBoxItem pathBoxItem ||
				pathBoxItem.Path == "Home" ||
				pathBoxItem.Path == "ReleaseNotes" ||
				pathBoxItem.Path == "Settings")
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

			_lockFlag = false;
		}

		[Obsolete("Superseded by Omnibar.")]
		public async Task PathBoxItem_DragOver(object sender, DragEventArgs e)
		{
			if (IsSingleItemOverride ||
				((FrameworkElement)sender).DataContext is not PathBoxItem pathBoxItem ||
				pathBoxItem.Path == "Home" ||
				pathBoxItem.Path == "ReleaseNotes" ||
				pathBoxItem.Path == "Settings")
			{
				return;
			}

			if (_dragOverPath != pathBoxItem.Path)
			{
				_dragOverPath = pathBoxItem.Path;
				_dragOverTimer.Stop();

				if (_dragOverPath != (this as IAddressToolbarViewModel).PathComponents.LastOrDefault()?.Path)
				{
					_dragOverTimer.Debounce(() =>
					{
						if (_dragOverPath is not null)
						{
							_dragOverTimer.Stop();
							ItemDraggedOverPathItem?.Invoke(this, new PathNavigationEventArgs()
							{
								ItemPath = _dragOverPath
							});
							_dragOverPath = null;
						}
					},
					TimeSpan.FromMilliseconds(Constants.DragAndDrop.HoverToOpenTimespan), false);
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
				e.DragUIOverride.Caption = string.Format(Strings.CopyToFolderCaptionText.GetLocalizedResource(), pathBoxItem.Title);
				e.AcceptedOperation = DataPackageOperation.Copy;
			}
			else
			{
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.Caption = string.Format(Strings.MoveToFolderCaptionText.GetLocalizedResource(), pathBoxItem.Title);
				// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
				e.AcceptedOperation = DataPackageOperation.Move | DataPackageOperation.Copy;
			}

			deferral.Complete();
		}

		[Obsolete("Superseded by Omnibar.")]
		public void CurrentPathSetTextBox_TextChanged(object sender, TextChangedEventArgs args)
		{
			if (sender is TextBox textBox)
				PathBoxQuerySubmitted?.Invoke(this, new ToolbarQuerySubmittedEventArgs() { QueryText = textBox.Text });
		}

		public async Task HandleFolderNavigationAsync(string path, bool openNewTab = false)
		{
			openNewTab |= _pointerRoutedEventArgs is not null;
			if (openNewTab)
			{
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(
					async () =>
					{
						await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), path, true);
					},
					DispatcherQueuePriority.Low);

				_pointerRoutedEventArgs = null;

				return;
			}

			ToolbarPathItemInvoked?.Invoke(this, new() { ItemPath = path });
		}

		public async Task HandleItemNavigationAsync(string path)
		{
			if (ContentPageContext.ShellPage is null || PathComponents.LastOrDefault()?.Path is not { } currentPath)
				return;

			var isFtp = FtpHelpers.IsFtpPath(path);
			var normalizedInput = NormalizePathInput(path, isFtp);
			if (currentPath.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase) ||
				string.IsNullOrWhiteSpace(normalizedInput))
				return;

			if (normalizedInput.Equals(ContentPageContext.ShellPage.ShellViewModel.WorkingDirectory) &&
				ContentPageContext.ShellPage.CurrentPageType != typeof(HomePage))
				return;

			if (normalizedInput.Equals("Home", StringComparison.OrdinalIgnoreCase) ||
				normalizedInput.Equals(Strings.Home.GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
			{
				SavePathToHistory("Home");
				ContentPageContext.ShellPage.NavigateHome();
			}
			else if (normalizedInput.Equals("ReleaseNotes", StringComparison.OrdinalIgnoreCase) ||
				normalizedInput.Equals(Strings.ReleaseNotes.GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
			{
				SavePathToHistory("ReleaseNotes");
				ContentPageContext.ShellPage.NavigateToReleaseNotes();
			}
			else if (normalizedInput.Equals("Settings", StringComparison.OrdinalIgnoreCase) ||
				normalizedInput.Equals(Strings.Settings.GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
			{
				//SavePathToHistory("Settings");
				//ContentPageContext.ShellPage.NavigateToSettings();
			}
			else
			{
				normalizedInput = StorageFileExtensions.GetResolvedPath(normalizedInput, isFtp);
				if (currentPath.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase))
					return;

				var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(normalizedInput));

				var resFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(normalizedInput, item));
				if (resFolder || FolderHelpers.CheckFolderAccessWithWin32(normalizedInput))
				{
					var matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x => PathNormalization.NormalizePath(normalizedInput).StartsWith(PathNormalization.NormalizePath(x.Path), StringComparison.Ordinal));
					if (matchingDrive is not null && matchingDrive.Type == Data.Items.DriveType.CDRom && matchingDrive.MaxSpace == ByteSizeLib.ByteSize.FromBytes(0))
					{
						bool ejectButton = await DialogDisplayHelper.ShowDialogAsync(Strings.InsertDiscDialog_Title.GetLocalizedResource(), string.Format(Strings.InsertDiscDialog_Text.GetLocalizedResource(), matchingDrive.Path), Strings.InsertDiscDialog_OpenDriveButton.GetLocalizedResource(), Strings.Close.GetLocalizedResource());
						if (ejectButton)
							DriveHelpers.EjectDeviceAsync(matchingDrive.Path);
						return;
					}

					var pathToNavigate = resFolder.Result?.Path ?? normalizedInput;
					SavePathToHistory(pathToNavigate);
					ContentPageContext.ShellPage.NavigateToPath(pathToNavigate);
				}
				else if (isFtp)
				{
					SavePathToHistory(normalizedInput);
					ContentPageContext.ShellPage.NavigateToPath(normalizedInput);
				}
				else // Not a folder or inaccessible
				{
					var resFile = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(normalizedInput, item));
					if (resFile)
					{
						var pathToInvoke = resFile.Result.Path;
						await Win32Helper.InvokeWin32ComponentAsync(pathToInvoke, ContentPageContext.ShellPage);
					}
					else // Not a file or not accessible
					{
						var workingDir =
							string.IsNullOrEmpty(ContentPageContext.ShellPage.ShellViewModel.WorkingDirectory) ||
							ContentPageContext.ShellPage.CurrentPageType == typeof(HomePage)
								? Constants.UserEnvironmentPaths.HomePath
								: ContentPageContext.ShellPage.ShellViewModel.WorkingDirectory;

						if (await LaunchApplicationFromPath(PathText, workingDir))
							return;

						try
						{
							if (!await Windows.System.Launcher.LaunchUriAsync(new Uri(PathText)))
								await DialogDisplayHelper.ShowDialogAsync(Strings.InvalidItemDialogTitle.GetLocalizedResource(),
									string.Format(Strings.InvalidItemDialogContent.GetLocalizedResource(), Environment.NewLine, resFolder.ErrorCode.ToString()));
						}
						catch (Exception ex) when (ex is UriFormatException || ex is ArgumentException)
						{
							await DialogDisplayHelper.ShowDialogAsync(Strings.InvalidItemDialogTitle.GetLocalizedResource(),
								string.Format(Strings.InvalidItemDialogContent.GetLocalizedResource(), Environment.NewLine, resFolder.ErrorCode.ToString()));
						}
					}
				}
			}

			PathControlDisplayText = ContentPageContext.ShellPage.ShellViewModel.WorkingDirectory;
		}

		public void SwitchToCommandPaletteMode()
		{
			OmnibarCurrentSelectedModeName = OmnibarPaletteModeName;
		}

		public void SwitchToSearchMode()
		{
			OmnibarCurrentSelectedModeName = OmnibarSearchModeName;
		}

		public void SwitchToPathMode()
		{
			OmnibarCurrentSelectedModeName = OmnibarPathModeName;

			var omnibar = AddressToolbar?.FindDescendant("Omnibar") as Omnibar;
			omnibar?.Focus(FocusState.Programmatic);
			omnibar.IsFocused = true;
		}

		public void UpdateAdditionalActions()
		{
			OnPropertyChanged(nameof(HasAdditionalAction));
		}

		public async Task SetPathBoxDropDownFlyoutAsync(MenuFlyout flyout, PathBoxItem pathItem)
		{
			var nextPathItemTitle = PathComponents[PathComponents.IndexOf(pathItem) + 1].Title;
			IList<StorageFolderWithPath>? childFolders = null;

			StorageFolderWithPath folder = await ContentPageContext.ShellPage.ShellViewModel.GetFolderWithPathFromPathAsync(pathItem.Path);
			if (folder is not null)
				childFolders = (await FilesystemTasks.Wrap(() => folder.GetFoldersWithPathAsync(string.Empty))).Result;

			flyout.Items?.Clear();

			if (childFolders is null || childFolders.Count == 0)
			{
				var flyoutItem = new MenuFlyoutItem
				{
					Icon = new FontIcon { Glyph = "\uE7BA" },
					Text = Strings.SubDirectoryAccessDenied.GetLocalizedResource(),
					//Foreground = (SolidColorBrush)Application.Current.Resources["SystemControlErrorTextForegroundBrush"],
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
				var flyoutItem = new MenuFlyoutItem
				{
					Icon = new FontIcon { Glyph = "\uE8B7" }, // Use font icon as placeholder
					Text = childFolder.Item.Name,
				};

				if (workingPath != childFolder.Path)
				{
					flyoutItem.Click += (sender, args) =>
					{
						// Navigate to the directory
						ContentPageContext.ShellPage.NavigateToPath(childFolder.Path);
					};
				}

				flyout.Items?.Add(flyoutItem);

				// Start loading the thumbnail in the background
				_ = LoadFlyoutItemIconAsync(flyoutItem, childFolder.Path);
			}
		}

		private async Task LoadFlyoutItemIconAsync(MenuFlyoutItem flyoutItem, string path)
		{
			var imageSource = await NavigationHelpers.GetIconForPathAsync(path);

			if (imageSource is not null)
				flyoutItem.Icon = new ImageIcon { Source = imageSource };
		}

		private static string NormalizePathInput(string currentInput, bool isFtp)
		{
			if (currentInput.Contains('/') && !isFtp)
				currentInput = currentInput.Replace("/", "\\", StringComparison.Ordinal);

			currentInput = currentInput.Replace("\\\\", "\\", StringComparison.Ordinal);

			if (currentInput.StartsWith('\\') && !currentInput.StartsWith("\\\\", StringComparison.Ordinal))
				currentInput = currentInput.Insert(0, "\\");

			return currentInput;
		}

		[Obsolete("Superseded by Omnibar.")]
		public async Task CheckPathInputAsync(string currentInput, string currentSelectedPath, IShellPage shellPage)
		{
			if (currentInput.StartsWith('>'))
			{
				var code = currentInput.Substring(1).Trim();
				var command = Commands[code];

				if (command == Commands.None)
					await DialogDisplayHelper.ShowDialogAsync(Strings.InvalidCommand.GetLocalizedResource(),
						string.Format(Strings.InvalidCommandContent.GetLocalizedResource(), code));
				else if (!command.IsExecutable)
					await DialogDisplayHelper.ShowDialogAsync(Strings.CommandNotExecutable.GetLocalizedResource(),
						string.Format(Strings.CommandNotExecutableContent.GetLocalizedResource(), command.Code));
				else
					await command.ExecuteAsync();

				return;
			}

			var isFtp = FtpHelpers.IsFtpPath(currentInput);

			var normalizedInput = NormalizePathInput(currentInput, isFtp);

			if (currentSelectedPath == normalizedInput || string.IsNullOrWhiteSpace(normalizedInput))
				return;

			if (normalizedInput != shellPage.ShellViewModel.WorkingDirectory || shellPage.CurrentPageType == typeof(HomePage))
			{
				if (normalizedInput.Equals("Home", StringComparison.OrdinalIgnoreCase) || normalizedInput.Equals(Strings.Home.GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
				{
					SavePathToHistory("Home");
					shellPage.NavigateHome();
				}
				else if (normalizedInput.Equals("ReleaseNotes", StringComparison.OrdinalIgnoreCase) || normalizedInput.Equals(Strings.ReleaseNotes.GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
				{
					SavePathToHistory("ReleaseNotes");
					shellPage.NavigateToReleaseNotes();
				}
				// TODO add settings page
				//else if (normalizedInput.Equals("Settings", StringComparison.OrdinalIgnoreCase) || normalizedInput.Equals(Strings.Settings.GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
				//{
				//	SavePathToHistory("Settings");
				//	shellPage.NavigateToReleaseNotes();
				//}
				else
				{
					normalizedInput = StorageFileExtensions.GetResolvedPath(normalizedInput, isFtp);
					if (currentSelectedPath == normalizedInput)
						return;

					var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(normalizedInput));

					var resFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(normalizedInput, item));
					if (resFolder || FolderHelpers.CheckFolderAccessWithWin32(normalizedInput))
					{
						var matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x => PathNormalization.NormalizePath(normalizedInput).StartsWith(PathNormalization.NormalizePath(x.Path), StringComparison.Ordinal));
						if (matchingDrive is not null && matchingDrive.Type == Data.Items.DriveType.CDRom && matchingDrive.MaxSpace == ByteSizeLib.ByteSize.FromBytes(0))
						{
							bool ejectButton = await DialogDisplayHelper.ShowDialogAsync(Strings.InsertDiscDialog_Title.GetLocalizedResource(), string.Format(Strings.InsertDiscDialog_Text.GetLocalizedResource(), matchingDrive.Path), Strings.InsertDiscDialog_OpenDriveButton.GetLocalizedResource(), Strings.Close.GetLocalizedResource());
							if (ejectButton)
								DriveHelpers.EjectDeviceAsync(matchingDrive.Path);
							return;
						}
						var pathToNavigate = resFolder.Result?.Path ?? normalizedInput;
						SavePathToHistory(pathToNavigate);
						shellPage.NavigateToPath(pathToNavigate);
					}
					else if (isFtp)
					{
						SavePathToHistory(normalizedInput);
						shellPage.NavigateToPath(normalizedInput);
					}
					else // Not a folder or inaccessible
					{
						var resFile = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(normalizedInput, item));
						if (resFile)
						{
							var pathToInvoke = resFile.Result.Path;
							await Win32Helper.InvokeWin32ComponentAsync(pathToInvoke, shellPage);
						}
						else // Not a file or not accessible
						{
							var workingDir =
								string.IsNullOrEmpty(shellPage.ShellViewModel.WorkingDirectory) ||
								shellPage.CurrentPageType == typeof(HomePage) ?
									Constants.UserEnvironmentPaths.HomePath :
									shellPage.ShellViewModel.WorkingDirectory;

							if (await LaunchApplicationFromPath(currentInput, workingDir))
								return;

							try
							{
								if (!await Windows.System.Launcher.LaunchUriAsync(new Uri(currentInput)))
									await DialogDisplayHelper.ShowDialogAsync(Strings.InvalidItemDialogTitle.GetLocalizedResource(),
										string.Format(Strings.InvalidItemDialogContent.GetLocalizedResource(), Environment.NewLine, resFolder.ErrorCode.ToString()));
							}
							catch (Exception ex) when (ex is UriFormatException || ex is ArgumentException)
							{
								await DialogDisplayHelper.ShowDialogAsync(Strings.InvalidItemDialogTitle.GetLocalizedResource(),
									string.Format(Strings.InvalidItemDialogContent.GetLocalizedResource(), Environment.NewLine, resFolder.ErrorCode.ToString()));
							}
						}
					}
				}

				PathControlDisplayText = shellPage.ShellViewModel.WorkingDirectory;
			}
		}

		private void SavePathToHistory(string path)
		{
			var pathHistoryList = UserSettingsService.GeneralSettingsService.PathHistoryList?.ToList() ?? [];
			pathHistoryList.Remove(path);
			pathHistoryList.Insert(0, path);

			if (pathHistoryList.Count > MaxSuggestionsCount)
				UserSettingsService.GeneralSettingsService.PathHistoryList = pathHistoryList.RemoveFrom(MaxSuggestionsCount + 1);
			else
				UserSettingsService.GeneralSettingsService.PathHistoryList = pathHistoryList;
		}

		public void SaveSearchQueryToList(string searchQuery)
		{
			var previousSearchQueriesList = UserSettingsService.GeneralSettingsService.PreviousSearchQueriesList?.ToList() ?? [];
			previousSearchQueriesList.Remove(searchQuery);
			previousSearchQueriesList.Insert(0, searchQuery);

			if (previousSearchQueriesList.Count > MaxSuggestionsCount)
				UserSettingsService.GeneralSettingsService.PreviousSearchQueriesList = previousSearchQueriesList.RemoveFrom(MaxSuggestionsCount + 1);
			else
				UserSettingsService.GeneralSettingsService.PreviousSearchQueriesList = previousSearchQueriesList;
		}

		private static async Task<bool> LaunchApplicationFromPath(string currentInput, string workingDir)
		{
			var args = CommandLineParser.SplitArguments(currentInput);
			return await LaunchHelper.LaunchAppAsync(
				args.FirstOrDefault("").Trim('"'), string.Join(' ', args.Skip(1)), workingDir
			);
		}

		public async Task PopulateOmnibarSuggestionsForPathMode()
		{
			var result = await SafetyExtensions.IgnoreExceptions((Func<Task<bool>>)(async () =>
			{
				List<OmnibarPathModeSuggestionModel>? newSuggestions = [];
				var pathText = this.PathText;

				// If the current input is special, populate navigation history instead.
				if (string.IsNullOrWhiteSpace((string)pathText) ||
					pathText is "Home" or "ReleaseNotes" or "Settings")
				{
					// Load previously entered path
					if (UserSettingsService.GeneralSettingsService.PathHistoryList is { } pathHistoryList)
					{
						newSuggestions.AddRange(pathHistoryList.Select(x => new OmnibarPathModeSuggestionModel(x, x)));
					}
				}
				else
				{
					var isFtp = FtpHelpers.IsFtpPath((string)pathText);
					pathText = NormalizePathInput((string)pathText, isFtp);
					var expandedPath = StorageFileExtensions.GetResolvedPath((string)pathText, isFtp);
					var folderPath = PathNormalization.GetParentDir(expandedPath) ?? expandedPath;
					StorageFolderWithPath folder = await ContentPageContext.ShellPage.ShellViewModel.GetFolderWithPathFromPathAsync(folderPath);
					if (folder is null)
						return false;

					var currPath = await folder.GetFoldersWithPathAsync(Path.GetFileName(expandedPath), MaxSuggestionsCount);
					if (currPath.Count >= MaxSuggestionsCount)
					{
						newSuggestions.AddRange(currPath.Select(x => new OmnibarPathModeSuggestionModel(x.Path, x.Item.DisplayName)));
					}
					else if (currPath.Any())
					{
						var subPath = await currPath.First().GetFoldersWithPathAsync((uint)(MaxSuggestionsCount - currPath.Count));
						newSuggestions.AddRange(currPath.Select(x => new OmnibarPathModeSuggestionModel(x.Path, x.Item.DisplayName)));
						newSuggestions.AddRange(subPath.Select(x => new OmnibarPathModeSuggestionModel(x.Path, PathNormalization.Combine(currPath.First().Item.DisplayName, x.Item.DisplayName))));
					}
				}

				// If there are no suggestions, show "No suggestions"
				if (newSuggestions.Count is 0)
					return false;

				// Check whether at least one item is in common between the old and the new suggestions
				// since the suggestions popup becoming empty causes flickering
				if (!PathModeSuggestionItems.IntersectBy(newSuggestions, x => x.DisplayName).Any())
				{
					// No items in common, update the list in-place
					for (int index = 0; index < newSuggestions.Count; index++)
					{
						if (index < PathModeSuggestionItems.Count)
						{
							PathModeSuggestionItems[index] = newSuggestions[index];
						}
						else
						{
							PathModeSuggestionItems.Add(newSuggestions[index]);
						}
					}

					while (PathModeSuggestionItems.Count > newSuggestions.Count)
						PathModeSuggestionItems.RemoveAt(PathModeSuggestionItems.Count - 1);
				}
				else
				{
					// At least an element in common, show animation
					foreach (var s in PathModeSuggestionItems.ExceptBy(newSuggestions, x => x.DisplayName).ToList())
						PathModeSuggestionItems.Remove(s);

					for (int index = 0; index < newSuggestions.Count; index++)
					{
						if (PathModeSuggestionItems.Count > index && PathModeSuggestionItems[index].DisplayName == newSuggestions[index].DisplayName)
						{
							PathModeSuggestionItems[index] = newSuggestions[index];
						}
						else
							PathModeSuggestionItems.Insert(index, newSuggestions[index]);
					}
				}

				return true;
			}));

			if (!result)
			{
				AddNoResultsItem();
			}

			void AddNoResultsItem()
			{
				PathModeSuggestionItems.Clear();
				PathModeSuggestionItems.Add(new(
					ContentPageContext.ShellPage.ShellViewModel.WorkingDirectory,
					Strings.NavigationToolbarVisiblePathNoResults.GetLocalizedResource()));
			}
		}

		public async Task PopulateOmnibarSuggestionsForCommandPaletteMode()
		{
			var newSuggestions = new List<NavigationBarSuggestionItem>();

			if (ContentPageContext.SelectedItems.Count == 1 && ContentPageContext.SelectedItem is not null && !ContentPageContext.SelectedItem.IsFolder)
			{
				try
				{
					var selectedItemPath = ContentPageContext.SelectedItem.ItemPath;
					var fileActionEntity = ActionManager.Instance.EntityFactory.CreateFileEntity(selectedItemPath);
					var actions = ActionManager.Instance.ActionRuntime.ActionCatalog.GetActionsForInputs(new[] { fileActionEntity });

					foreach (var action in actions.Where(a => a.Definition.Description.Contains(OmnibarCommandPaletteModeText, StringComparison.OrdinalIgnoreCase)))
					{
						var newItem = new NavigationBarSuggestionItem
						{
							PrimaryDisplay = action.Definition.Description,
							SearchText = OmnibarCommandPaletteModeText,
							ActionInstance = action
						};

						if (Uri.TryCreate(action.Definition.IconFullPath, UriKind.RelativeOrAbsolute, out Uri? validUri))
						{
							try
							{
								newItem.ActionIconSource = new BitmapImage(validUri);
							}
							catch (Exception)
							{
							}
						}

						newSuggestions.Add(newItem);
					}
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex, ex.Message);
				}
			}

			IEnumerable<NavigationBarSuggestionItem> suggestionItems = null!;

			await Task.Run(() =>
			{
				suggestionItems = Commands
					.Where(command => command.IsExecutable
						&& command.IsAccessibleGlobally
						&& (command.Description.Contains(OmnibarCommandPaletteModeText, StringComparison.OrdinalIgnoreCase)
							|| command.Code.ToString().Contains(OmnibarCommandPaletteModeText, StringComparison.OrdinalIgnoreCase)))
					.Select(command => new NavigationBarSuggestionItem
					{
						ThemedIconStyle = command.Glyph.ToThemedIconStyle(),
						Glyph = command.Glyph.BaseGlyph,
						Text = command.Description,
						PrimaryDisplay = command.Description,
						HotKeys = command.HotKeys,
						SearchText = OmnibarCommandPaletteModeText,
					})
					.Where(item => item.Text != Commands.OpenCommandPalette.Description.ToString()
						&& item.Text != Commands.EditPath.Description.ToString());
			});

			newSuggestions.AddRange(suggestionItems);

			if (newSuggestions.Count == 0)
			{
				newSuggestions.Add(new NavigationBarSuggestionItem()
				{
					PrimaryDisplay = string.Format(Strings.NoCommandsFound.GetLocalizedResource(), OmnibarCommandPaletteModeText),
					SearchText = OmnibarCommandPaletteModeText,
				});
			}

			if (!OmnibarCommandPaletteModeSuggestionItems.IntersectBy(newSuggestions, x => x.PrimaryDisplay).Any())
			{
				for (int index = 0; index < newSuggestions.Count; index++)
				{
					if (index < OmnibarCommandPaletteModeSuggestionItems.Count)
						OmnibarCommandPaletteModeSuggestionItems[index] = newSuggestions[index];
					else
						OmnibarCommandPaletteModeSuggestionItems.Add(newSuggestions[index]);
				}

				while (OmnibarCommandPaletteModeSuggestionItems.Count > newSuggestions.Count)
					OmnibarCommandPaletteModeSuggestionItems.RemoveAt(OmnibarCommandPaletteModeSuggestionItems.Count - 1);
			}
			else
			{
				foreach (var s in OmnibarCommandPaletteModeSuggestionItems.ExceptBy(newSuggestions, x => x.PrimaryDisplay).ToList())
					OmnibarCommandPaletteModeSuggestionItems.Remove(s);

				for (int index = 0; index < newSuggestions.Count; index++)
				{
					if (OmnibarCommandPaletteModeSuggestionItems.Count > index
						&& OmnibarCommandPaletteModeSuggestionItems[index].PrimaryDisplay == newSuggestions[index].PrimaryDisplay)
					{
						OmnibarCommandPaletteModeSuggestionItems[index] = newSuggestions[index];
					}
					else
					{
						OmnibarCommandPaletteModeSuggestionItems.Insert(index, newSuggestions[index]);
					}
				}
			}
		}

		public async Task PopulateOmnibarSuggestionsForSearchMode()
		{
			if (ContentPageContext.ShellPage is null)
				return;

			List<SuggestionModel> newSuggestions = [];

			if (string.IsNullOrWhiteSpace(OmnibarSearchModeText))
			{
				var previousSearchQueries = UserSettingsService.GeneralSettingsService.PreviousSearchQueriesList;
				if (previousSearchQueries is not null)
					newSuggestions.AddRange(previousSearchQueries.Select(query => new SuggestionModel(query, true)));
			}
			else
			{
				var search = new FolderSearch
				{
					Query = OmnibarSearchModeText,
					Folder = ContentPageContext.ShellPage.ShellViewModel.WorkingDirectory,
					MaxItemCount = 10,
				};

				var results = await search.SearchAsync();
				newSuggestions.AddRange(results.Select(result => new SuggestionModel(result)));
			}

			// Remove outdated suggestions
			var toRemove = OmnibarSearchModeSuggestionItems
				.Where(existing => !newSuggestions.Any(newItem => newItem.ItemPath == existing.ItemPath))
				.ToList();

			foreach (var item in toRemove)
				OmnibarSearchModeSuggestionItems.Remove(item);

			// Add new suggestions
			var toAdd = newSuggestions
				.Where(newItem => !OmnibarSearchModeSuggestionItems.Any(existing => existing.Name == newItem.Name));

			foreach (var item in toAdd)
				OmnibarSearchModeSuggestionItems.Add(item);
		}

		private void FolderSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(LayoutPreferencesManager.LayoutMode):
					LayoutThemedIcon = _InstanceViewModel.FolderSettings.LayoutMode switch
					{
						FolderLayoutModes.ListView => Commands.LayoutList.ThemedIconStyle!,
						FolderLayoutModes.CardsView => Commands.LayoutCards.ThemedIconStyle!,
						FolderLayoutModes.ColumnView => Commands.LayoutColumns.ThemedIconStyle!,
						FolderLayoutModes.GridView => Commands.LayoutGrid.ThemedIconStyle!,
						_ => Commands.LayoutDetails.ThemedIconStyle!
					};
					OnPropertyChanged(nameof(IsCardsLayout));
					OnPropertyChanged(nameof(IsListLayout));
					OnPropertyChanged(nameof(IsColumnLayout));
					OnPropertyChanged(nameof(IsGridLayout));
					OnPropertyChanged(nameof(IsDetailsLayout));
					OnPropertyChanged(nameof(IsLayoutSizeCompact));
					OnPropertyChanged(nameof(IsLayoutSizeSmall));
					OnPropertyChanged(nameof(IsLayoutSizeMedium));
					OnPropertyChanged(nameof(IsLayoutSizeLarge));
					OnPropertyChanged(nameof(IsLayoutSizeExtraLarge));
					break;
			}
		}

		// Disposer

		public void Dispose()
		{
			UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
		}
	}
}
