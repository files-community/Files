// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Actions;
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
	public sealed partial class NavigationToolbarViewModel : ObservableObject, IAddressToolbarViewModel, IDisposable
	{
		// Constants

		private const int MaxSuggestionsCount = 10;

		// Dependency injections

		private readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IAppearanceSettingsService AppearanceSettingsService = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
		private readonly IGeneralSettingsService GeneralSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private readonly IUpdateService UpdateService = Ioc.Default.GetRequiredService<IUpdateService>();
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();

		// Fields

		private readonly DispatcherQueue _dispatcherQueue;
		private readonly DispatcherQueueTimer _dragOverTimer;

		private string? _dragOverPath;
		private bool _lockFlag;
		private PointerRoutedEventArgs? _pointerRoutedEventArgs;

		// Events

		public delegate void ToolbarPathItemInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public delegate void ToolbarFlyoutOpeningEventHandler(object sender, ToolbarFlyoutOpeningEventArgs e);
		public delegate void ToolbarPathItemLoadedEventHandler(object sender, ToolbarPathItemLoadedEventArgs e);
		public delegate void AddressBarTextEnteredEventHandler(object sender, AddressBarTextEnteredEventArgs e);
		public delegate void PathBoxItemDroppedEventHandler(object sender, PathBoxItemDroppedEventArgs e);
		public event ToolbarPathItemInvokedEventHandler? ToolbarPathItemInvoked;
		public event ToolbarFlyoutOpeningEventHandler? ToolbarFlyoutOpening;
		public event ToolbarPathItemLoadedEventHandler? ToolbarPathItemLoaded;
		public event IAddressToolbarViewModel.ItemDraggedOverPathItemEventHandler? ItemDraggedOverPathItem;
		public event EventHandler? EditModeEnabled;
		public event IAddressToolbarViewModel.ToolbarQuerySubmittedEventHandler? PathBoxQuerySubmitted;
		public event AddressBarTextEnteredEventHandler? AddressBarTextEntered;
		public event PathBoxItemDroppedEventHandler? PathBoxItemDropped;
		public event EventHandler? RefreshWidgetsRequested;

		// Properties

		public ObservableCollection<PathBoxItem> PathComponents { get; } = [];

		public ObservableCollection<NavigationBarSuggestionItem> NavigationBarSuggestions { get; } = [];

		public bool IsSingleItemOverride { get; set; }

		public bool SearchHasFocus { get; private set; }

		public bool ShowHomeButton => AppearanceSettingsService.ShowHomeButton;
		public bool EnableOmnibar => GeneralSettingsService.EnableOmnibar;

		public bool ShowShelfPaneToggleButton => AppearanceSettingsService.ShowShelfPaneToggleButton && AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev;

		private NavigationToolbar? AddressToolbar => (MainWindow.Instance.Content as Frame)?.FindDescendant<NavigationToolbar>();

		public SearchBoxViewModel SearchBoxViewModel => (SearchBoxViewModel)SearchBox;

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

		private bool _IsCommandPaletteOpen;
		public bool IsCommandPaletteOpen { get => _IsCommandPaletteOpen; set => SetProperty(ref _IsCommandPaletteOpen, value); }

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

		private string _SearchButtonGlyph = "\uE721";
		public string SearchButtonGlyph { get => _SearchButtonGlyph; set => SetProperty(ref _SearchButtonGlyph, value); }

		private bool _ManualEntryBoxLoaded;
		public bool ManualEntryBoxLoaded { get => _ManualEntryBoxLoaded; set => SetProperty(ref _ManualEntryBoxLoaded, value); }

		private bool _ClickablePathLoaded = true;
		public bool ClickablePathLoaded { get => _ClickablePathLoaded; set => SetProperty(ref _ClickablePathLoaded, value); }

		private string _PathControlDisplayText;
		public string PathControlDisplayText { get => _PathControlDisplayText; set => SetProperty(ref _PathControlDisplayText, value); }

		private bool _HasItem = false;
		public bool HasItem { get => _HasItem; set => SetProperty(ref _HasItem, value); }

		private Style _LayoutThemedIcon;
		public Style LayoutThemedIcon { get => _LayoutThemedIcon; set => SetProperty(ref _LayoutThemedIcon, value); }

		private ISearchBoxViewModel _SearchBox = new SearchBoxViewModel();
		public ISearchBoxViewModel SearchBox { get => _SearchBox; set => SetProperty(ref _SearchBox, value); }

		private bool _IsSearchBoxVisible;
		public bool IsSearchBoxVisible
		{
			get => _IsSearchBoxVisible;
			set
			{
				if (SetProperty(ref _IsSearchBoxVisible, value))
					SearchButtonGlyph = value ? "\uE711" : "\uE721";
			}
		}

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

			SearchBox.Escaped += SearchRegion_Escaped;
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
					case nameof(AppearanceSettingsService.ShowHomeButton):
						OnPropertyChanged(nameof(ShowHomeButton));
						break;
					case nameof(AppearanceSettingsService.ShowShelfPaneToggleButton):
						OnPropertyChanged(nameof(ShowShelfPaneToggleButton));
						break;
				}
			};
			GeneralSettingsService.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(GeneralSettingsService.EnableOmnibar):
						OnPropertyChanged(nameof(EnableOmnibar));
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

		public void PathBoxItem_DragLeave(object sender, DragEventArgs e)
		{
			if (((StackPanel)sender).DataContext is not PathBoxItem pathBoxItem ||
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

		public async Task PathBoxItem_Drop(object sender, DragEventArgs e)
		{
			if (_lockFlag)
				return;

			_lockFlag = true;

			// Reset dragged over pathbox item
			_dragOverPath = null;

			if (((StackPanel)sender).DataContext is not PathBoxItem pathBoxItem ||
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

		public async Task PathBoxItem_DragOver(object sender, DragEventArgs e)
		{
			if (IsSingleItemOverride ||
				((StackPanel)sender).DataContext is not PathBoxItem pathBoxItem ||
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

		public void PathboxItemFlyout_Opening(object sender, object e)
		{
			ToolbarFlyoutOpening?.Invoke(this, new ToolbarFlyoutOpeningEventArgs((MenuFlyout)sender));
		}

		public void PathBoxItemFlyout_Closed(object sender, object e)
		{
			((MenuFlyout)sender).Items.Clear();
		}

		public void CurrentPathSetTextBox_TextChanged(object sender, TextChangedEventArgs args)
		{
			if (sender is TextBox textBox)
				PathBoxQuerySubmitted?.Invoke(this, new ToolbarQuerySubmittedEventArgs() { QueryText = textBox.Text });
		}

		public void VisiblePath_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				AddressBarTextEntered?.Invoke(this, new AddressBarTextEnteredEventArgs() { AddressBarTextField = sender });
		}

		public void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			PathBoxQuerySubmitted?.Invoke(this, new ToolbarQuerySubmittedEventArgs() { QueryText = args.QueryText });

			(this as IAddressToolbarViewModel).IsEditModeEnabled = false;
		}

		public void PathBoxItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Mouse)
				return;

			var ptrPt = e.GetCurrentPoint(AddressToolbar);
			_pointerRoutedEventArgs = ptrPt.Properties.IsMiddleButtonPressed ? e : null;
		}

		public async Task PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var itemTappedPath = ((sender as TextBlock)?.DataContext as PathBoxItem)?.Path;
			if (itemTappedPath is null)
				return;

			if (_pointerRoutedEventArgs is not null)
			{
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), itemTappedPath, true);
				}, DispatcherQueuePriority.Low);
				e.Handled = true;
				_pointerRoutedEventArgs = null;

				return;
			}

			ToolbarPathItemInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = itemTappedPath
			});
		}

		public void PathBoxItem_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			switch (e.Key)
			{
				case Windows.System.VirtualKey.Down:
				{
					var item = e.OriginalSource as ListViewItem;
					var button = item?.FindDescendant<Button>();
					button?.Flyout.ShowAt(button);
					e.Handled = true;
					break;
				}
				case Windows.System.VirtualKey.Space: 
				case Windows.System.VirtualKey.Enter:
				{
					var item = e.OriginalSource as ListViewItem;
					var path = (item?.Content as PathBoxItem)?.Path;
					if (path == PathControlDisplayText)
						return;
					ToolbarPathItemInvoked?.Invoke(this, new PathNavigationEventArgs()
					{
						ItemPath = path
					});
					e.Handled = true;
					break;
				}
			}
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

		private void CloseSearchBox(bool doFocus = false)
		{
			if (_SearchBox.WasQuerySubmitted)
			{
				_SearchBox.WasQuerySubmitted = false;
			}
			else
			{
				IsSearchBoxVisible = false;

				if (doFocus)
				{
					SearchBox.Query = string.Empty;

					var page = Ioc.Default.GetRequiredService<IContentPageContext>().ShellPage?.SlimContentPage;

					if (page is BaseGroupableLayoutPage svb && svb.IsLoaded)
						page.ItemManipulationModel.FocusFileList();
					else
						AddressToolbar?.Focus(FocusState.Programmatic);
				}
			}
		}

		public void SearchRegion_GotFocus(object sender, RoutedEventArgs e)
		{
			SearchHasFocus = true;
		}

		public void SearchRegion_LostFocus(object sender, RoutedEventArgs e)
		{
			var element = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement();
			if (element is FlyoutBase or AppBarButton)
				return;

			SearchHasFocus = false;
			CloseSearchBox();
		}

		private void SearchRegion_Escaped(object? sender, ISearchBoxViewModel _SearchBox)
			=> CloseSearchBox(true);

		public async Task SetPathBoxDropDownFlyoutAsync(MenuFlyout flyout, PathBoxItem pathItem, IShellPage shellPage)
		{
			var nextPathItemTitle = PathComponents[PathComponents.IndexOf(pathItem) + 1].Title;
			IList<StorageFolderWithPath>? childFolders = null;

			StorageFolderWithPath folder = await shellPage.ShellViewModel.GetFolderWithPathFromPathAsync(pathItem.Path);
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
				var flyoutItem = new MenuFlyoutItem
				{
					Icon = new FontIcon { Glyph = "\uE8B7" }, // Use font icon as placeholder
					Text = childFolder.Item.Name,
					FontSize = 12,
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

		private static async Task<bool> LaunchApplicationFromPath(string currentInput, string workingDir)
		{
			var args = CommandLineParser.SplitArguments(currentInput);
			return await LaunchHelper.LaunchAppAsync(
				args.FirstOrDefault("").Trim('"'), string.Join(' ', args.Skip(1)), workingDir
			);
		}

		public async Task SetAddressBarSuggestionsAsync(AutoSuggestBox sender, IShellPage shellpage)
		{
			if (sender.Text is not null && shellpage.ShellViewModel is not null)
			{
				if (!await SafetyExtensions.IgnoreExceptions(async () =>
				{
					IList<NavigationBarSuggestionItem>? suggestions = null;

					if (sender.Text.StartsWith(">"))
					{
						IsCommandPaletteOpen = true;
						var searchText = sender.Text.Substring(1).Trim();
						suggestions = Commands.Where(command =>
							command.IsExecutable &&
							command.IsAccessibleGlobally &&
							(command.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
							command.Code.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)))
						.Select(command => new NavigationBarSuggestionItem()
						{
							Text = ">" + command.Code,
							PrimaryDisplay = command.Description,
							HotKeys = command.HotKeys,
							SearchText = searchText,
						}).ToList();
					}
					else
					{
						IsCommandPaletteOpen = false;
						var currentInput = sender.Text;

						if (string.IsNullOrWhiteSpace(currentInput) || currentInput == "Home" || currentInput == "ReleaseNotes" || currentInput == "Settings")
						{
							// Load previously entered path
							var pathHistoryList = UserSettingsService.GeneralSettingsService.PathHistoryList;
							if (pathHistoryList is not null)
							{
								suggestions = pathHistoryList.Select(x => new NavigationBarSuggestionItem()
								{
									Text = x,
									PrimaryDisplay = x
								}).ToList();
							}
						}
						else
						{
							var isFtp = FtpHelpers.IsFtpPath(currentInput);
							currentInput = NormalizePathInput(currentInput, isFtp);
							var expandedPath = StorageFileExtensions.GetResolvedPath(currentInput, isFtp);
							var folderPath = PathNormalization.GetParentDir(expandedPath) ?? expandedPath;
							StorageFolderWithPath folder = await shellpage.ShellViewModel.GetFolderWithPathFromPathAsync(folderPath);

							if (folder is null)
								return false;

							var currPath = await folder.GetFoldersWithPathAsync(Path.GetFileName(expandedPath), (uint)MaxSuggestionsCount);
							if (currPath.Count >= MaxSuggestionsCount)
							{
								suggestions = currPath.Select(x => new NavigationBarSuggestionItem()
								{
									Text = x.Path,
									PrimaryDisplay = x.Item.DisplayName
								}).ToList();
							}
							else if (currPath.Any())
							{
								var subPath = await currPath.First().GetFoldersWithPathAsync((uint)(MaxSuggestionsCount - currPath.Count));
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
					}

					if (suggestions is null || suggestions.Count == 0)
					{
						suggestions = new List<NavigationBarSuggestionItem>() { new NavigationBarSuggestionItem() {
						Text = shellpage.ShellViewModel.WorkingDirectory,
						PrimaryDisplay = Strings.NavigationToolbarVisiblePathNoResults.GetLocalizedResource() } };
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
								NavigationBarSuggestions[index].SearchText = suggestions[index].SearchText;
								NavigationBarSuggestions[index].HotKeys = suggestions[index].HotKeys;
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
							{
								NavigationBarSuggestions[index].SearchText = suggestions[index].SearchText;
								NavigationBarSuggestions[index].HotKeys = suggestions[index].HotKeys;
							}
							else
								NavigationBarSuggestions.Insert(index, suggestions[index]);
						}
					}

					return true;
				}))
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						NavigationBarSuggestions.Clear();
						NavigationBarSuggestions.Add(new NavigationBarSuggestionItem()
						{
							Text = shellpage.ShellViewModel.WorkingDirectory,
							PrimaryDisplay = Strings.NavigationToolbarVisiblePathNoResults.GetLocalizedResource()
						});
					});
				}
			}
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
			SearchBox.Escaped -= SearchRegion_Escaped;
			UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
		}
	}
}
