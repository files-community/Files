using Files.Common;
using Files.Enums;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Services;
using Files.UserControls;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using static Files.UserControls.INavigationToolbar;
using SearchBox = Files.UserControls.SearchBox;
using SortDirection = Files.Enums.SortDirection;

namespace Files.ViewModels
{
    public class NavToolbarViewModel : ObservableObject, INavigationToolbar, IDisposable
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public delegate void ToolbarPathItemInvokedEventHandler(object sender, PathNavigationEventArgs e);

        public delegate void ToolbarFlyoutOpenedEventHandler(object sender, ToolbarFlyoutOpenedEventArgs e);

        public delegate void ToolbarPathItemLoadedEventHandler(object sender, ToolbarPathItemLoadedEventArgs e);

        public delegate void AddressBarTextEnteredEventHandler(object sender, AddressBarTextEnteredEventArgs e);

        public delegate void PathBoxItemDroppedEventHandler(object sender, PathBoxItemDroppedEventArgs e);

        public event ToolbarPathItemInvokedEventHandler ToolbarPathItemInvoked;

        public event ToolbarFlyoutOpenedEventHandler ToolbarFlyoutOpened;

        public event ToolbarPathItemLoadedEventHandler ToolbarPathItemLoaded;

        public event ItemDraggedOverPathItemEventHandler ItemDraggedOverPathItem;

        public event EventHandler EditModeEnabled;

        public event ToolbarQuerySubmittedEventHandler PathBoxQuerySubmitted;

        public event AddressBarTextEnteredEventHandler AddressBarTextEntered;

        public event PathBoxItemDroppedEventHandler PathBoxItemDropped;

        public event EventHandler BackRequested;

        public event EventHandler ForwardRequested;

        public event EventHandler UpRequested;

        public event EventHandler RefreshRequested;

        public event EventHandler RefreshWidgetsRequested;

        public ObservableCollection<PathBoxItem> PathComponents { get; } = new ObservableCollection<PathBoxItem>();

        public bool IsSortedAscending
        {
            get => InstanceViewModel?.FolderSettings.DirectorySortDirection == SortDirection.Ascending;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortDirection = SortDirection.Ascending; }
        }

        public bool IsSortedDescending
        {
            get => InstanceViewModel?.FolderSettings.DirectorySortDirection == SortDirection.Descending;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortDirection = SortDirection.Descending; }
        }

        // Sort by

        public bool IsSortedByName
        {
            get => InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.Name;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.Name; OnPropertyChanged(); }
        }

        public bool IsSortedByDateModified
        {
            get => InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.DateModified;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.DateModified; OnPropertyChanged(); }
        }

        public bool IsSortedByDateCreated
        {
            get => InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.DateCreated;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.DateCreated; OnPropertyChanged(); }
        }

        public bool IsSortedBySize
        {
            get => InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.Size;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.Size; OnPropertyChanged(); }
        }

        public bool IsSortedByType
        {
            get => InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.FileType;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.FileType; OnPropertyChanged(); }
        }

        public bool IsSortedBySyncStatus
        {
            get => InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.SyncStatus;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.SyncStatus; OnPropertyChanged(); }
        }

        public bool IsSortedByOriginalFolder
        {
            get => InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.OriginalFolder;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.OriginalFolder; OnPropertyChanged(); }
        }

        public bool IsSortedByDateDeleted
        {
            get => InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.DateDeleted;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.DateDeleted; OnPropertyChanged(); }
        }

        public bool IsSortedByFileTag
        {
            get => InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.FileTag;
            set { if (value) InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.FileTag; OnPropertyChanged(); }
        }

        // Group by

        public bool IsGroupedByNone
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.None;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.None; OnPropertyChanged(); }
        }

        public bool IsGroupedByName
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.Name;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.Name; OnPropertyChanged(); }
        }

        public bool IsGroupedByDateModified
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.DateModified;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.DateModified; OnPropertyChanged(); }
        }

        public bool IsGroupedByDateCreated
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.DateCreated;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.DateCreated; OnPropertyChanged(); }
        }

        public bool IsGroupedBySize
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.Size;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.Size; OnPropertyChanged(); }
        }

        public bool IsGroupedByType
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FileType;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.FileType; OnPropertyChanged(); }
        }

        public bool IsGroupedBySyncStatus
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.SyncStatus;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.SyncStatus; OnPropertyChanged(); }
        }

        public bool IsGroupedByOriginalFolder
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.OriginalFolder;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.OriginalFolder; OnPropertyChanged(); }
        }

        public bool IsGroupedByDateDeleted
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.DateDeleted;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.DateDeleted; OnPropertyChanged(); }
        }

        public bool IsGroupedByFileTag
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FileTag;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.FileTag; OnPropertyChanged(); }
        }

        public bool IsGroupedByFolderPath
        {
            get => InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FolderPath;
            set { if (value) InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.FolderPath; OnPropertyChanged(); }
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
                {
                    SearchButtonGlyph = value ? "\uE711" : "\uE721";
                }
            }
        }

        private string pathText;

        public string PathText
        {
            get => pathText;
            set
            {
                pathText = value;
                OnPropertyChanged(nameof(PathText));
            }
        }

        public ObservableCollection<ListedItem> NavigationBarSuggestions = new ObservableCollection<ListedItem>();

        private CurrentInstanceViewModel instanceViewModel;

        public CurrentInstanceViewModel InstanceViewModel
        {
            get => instanceViewModel;
            set
            {
                if (instanceViewModel != value)
                {
                    if (instanceViewModel != null)
                    {
                        InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
                        InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
                        InstanceViewModel.FolderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
                    }

                    SetProperty(ref instanceViewModel, value);

                    if (instanceViewModel != null)
                    {
                        InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated += FolderSettings_SortDirectionPreferenceUpdated;
                        InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated += FolderSettings_SortOptionPreferenceUpdated;
                        InstanceViewModel.FolderSettings.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
                    }
                }
            }
        }

        private PointerRoutedEventArgs pointerRoutedEventArgs;

        public NavToolbarViewModel()
        {
            dragOverTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            SearchBox.Escaped += SearchRegion_Escaped;
            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
        }

        private void UserSettingsService_OnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(ShowFoldersWidget):
                case nameof(ShowDrivesWidget):
                case nameof(ShowBundlesWidget):
                case nameof(ShowRecentFilesWidget):
                    RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
                    OnPropertyChanged(e.settingName);
                    break;
            }
        }

        private DispatcherQueueTimer dragOverTimer;

        private ISearchBox searchBox = new SearchBoxViewModel();

        public ISearchBox SearchBox
        {
            get => searchBox;
            set => SetProperty(ref searchBox, value);
        }

        public SearchBoxViewModel SearchBoxViewModel => SearchBox as SearchBoxViewModel;

        public bool IsSingleItemOverride { get; set; } = false;

        private string dragOverPath = null;

        public void UpdateSortAndGroupOptions()
        {
            FolderSettings_SortDirectionPreferenceUpdated(null, 0);
            FolderSettings_SortOptionPreferenceUpdated(null, 0);
            FolderSettings_GroupOptionPreferenceUpdated(null, 0);
        }

        private void FolderSettings_SortDirectionPreferenceUpdated(object sender, SortDirection e)
        {
            OnPropertyChanged(nameof(IsSortedAscending));
            OnPropertyChanged(nameof(IsSortedDescending));
        }

        private void FolderSettings_SortOptionPreferenceUpdated(object sender, SortOption e)
        {
            OnPropertyChanged(nameof(IsSortedByName));
            OnPropertyChanged(nameof(IsSortedByDateModified));
            OnPropertyChanged(nameof(IsSortedByDateCreated));
            OnPropertyChanged(nameof(IsSortedBySize));
            OnPropertyChanged(nameof(IsSortedByType));
            OnPropertyChanged(nameof(IsSortedBySyncStatus));
            OnPropertyChanged(nameof(IsSortedByOriginalFolder));
            OnPropertyChanged(nameof(IsSortedByDateDeleted));
            OnPropertyChanged(nameof(IsSortedByFileTag));
        }

        private void FolderSettings_GroupOptionPreferenceUpdated(object sender, GroupOption e)
        {
            OnPropertyChanged(nameof(IsGroupedByNone));
            OnPropertyChanged(nameof(IsGroupedByName));
            OnPropertyChanged(nameof(IsGroupedByDateModified));
            OnPropertyChanged(nameof(IsGroupedByDateCreated));
            OnPropertyChanged(nameof(IsGroupedBySize));
            OnPropertyChanged(nameof(IsGroupedByType));
            OnPropertyChanged(nameof(IsGroupedBySyncStatus));
            OnPropertyChanged(nameof(IsGroupedByOriginalFolder));
            OnPropertyChanged(nameof(IsGroupedByDateDeleted));
            OnPropertyChanged(nameof(IsGroupedByFileTag));
            OnPropertyChanged(nameof(IsGroupedByFolderPath));
        }

        public void PathBoxItem_DragLeave(object sender, DragEventArgs e)
        {
            if (!((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home".GetLocalized())
            {
                return;
            }

            if (pathBoxItem.Path == dragOverPath)
            {
                // Reset dragged over pathbox item
                dragOverPath = null;
            }
        }

        private bool lockFlag = false;

        public async void PathBoxItem_Drop(object sender, DragEventArgs e)
        {
            if (lockFlag)
            {
                return;
            }
            lockFlag = true;

            dragOverPath = null; // Reset dragged over pathbox item

            if (!((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home".GetLocalized())
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

        public async void PathBoxItem_DragOver(object sender, DragEventArgs e)
        {
            if (IsSingleItemOverride || !((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home".GetLocalized())
            {
                return;
            }

            if (dragOverPath != pathBoxItem.Path)
            {
                dragOverPath = pathBoxItem.Path;
                dragOverTimer.Stop();
                if (dragOverPath != (this as INavigationToolbar).PathComponents.LastOrDefault()?.Path)
                {
                    dragOverTimer.Debounce(() =>
                    {
                        if (dragOverPath != null)
                        {
                            dragOverTimer.Stop();
                            ItemDraggedOverPathItem?.Invoke(this, new PathNavigationEventArgs()
                            {
                                ItemPath = dragOverPath
                            });
                            dragOverPath = null;
                        }
                    }, TimeSpan.FromMilliseconds(1000), false);
                }
            }

            if (!FilesystemHelpers.HasDraggedStorageItems(e.DataView))
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }
            if (string.IsNullOrEmpty(pathBoxItem.Path)) // In search page
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }

            e.Handled = true;
            var deferral = e.GetDeferral();

            var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
            var storageItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

            if (handledByFtp)
            {
                e.AcceptedOperation = DataPackageOperation.None;
                deferral.Complete();
                return;
            }

            if (!storageItems.Any(storageItem =>
                !string.IsNullOrEmpty(storageItem?.Path) &&
                storageItem.Path.Replace(pathBoxItem.Path, string.Empty, StringComparison.Ordinal).
                Trim(Path.DirectorySeparatorChar).
                Contains(Path.DirectorySeparatorChar)))
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            // copy be default when dragging from zip
            else if (storageItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
                || ZipStorageFolder.IsZipPath(pathBoxItem.Path))
            {
                e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), pathBoxItem.Title);
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), pathBoxItem.Title);
                e.AcceptedOperation = DataPackageOperation.Move;
            }

            deferral.Complete();
        }

        public bool IsEditModeEnabled
        {
            get
            {
                return ManualEntryBoxLoaded;
            }
            set
            {
                if (value)
                {
                    EditModeEnabled?.Invoke(this, EventArgs.Empty);

                    var visiblePath = NavToolbar.FindDescendant<AutoSuggestBox>(x => x.Name == "VisiblePath");
                    visiblePath?.Focus(FocusState.Programmatic);
                    visiblePath?.FindDescendant<TextBox>()?.SelectAll();

                    AddressBarTextEntered?.Invoke(this, new AddressBarTextEnteredEventArgs() { AddressBarTextField = visiblePath });
                }
                else
                {
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

        public ICommand BackClickCommand => new RelayCommand<RoutedEventArgs>(e => BackRequested?.Invoke(this, EventArgs.Empty));
        public ICommand ForwardClickCommand => new RelayCommand<RoutedEventArgs>(e => ForwardRequested?.Invoke(this, EventArgs.Empty));
        public ICommand UpClickCommand => new RelayCommand<RoutedEventArgs>(e => UpRequested?.Invoke(this, EventArgs.Empty));
        public ICommand RefreshClickCommand => new RelayCommand<RoutedEventArgs>(e => RefreshRequested?.Invoke(this, EventArgs.Empty));

        public void PathItemSeparator_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var pathSeparatorIcon = sender as FontIcon;
            if (pathSeparatorIcon.DataContext == null)
            {
                return;
            }
            ToolbarPathItemLoaded?.Invoke(pathSeparatorIcon, new ToolbarPathItemLoadedEventArgs()
            {
                Item = pathSeparatorIcon.DataContext as PathBoxItem,
                OpenedFlyout = pathSeparatorIcon.ContextFlyout as MenuFlyout
            });
        }

        public void PathboxItemFlyout_Opened(object sender, object e)
        {
            ToolbarFlyoutOpened?.Invoke(this, new ToolbarFlyoutOpenedEventArgs() { OpenedFlyout = sender as MenuFlyout });
        }

        public void VisiblePath_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                AddressBarTextEntered?.Invoke(this, new AddressBarTextEnteredEventArgs() { AddressBarTextField = sender });
            }
        }

        public void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            PathBoxQuerySubmitted?.Invoke(this, new ToolbarQuerySubmittedEventArgs() { QueryText = args.QueryText });

            (this as INavigationToolbar).IsEditModeEnabled = false;
        }

        public void PathBoxItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                Windows.UI.Input.PointerPoint ptrPt = e.GetCurrentPoint(NavToolbar);
                if (ptrPt.Properties.IsMiddleButtonPressed)
                {
                    pointerRoutedEventArgs = e;
                }
                else
                {
                    pointerRoutedEventArgs = null;
                }
            }
        }

        public async void PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var itemTappedPath = ((sender as Border).DataContext as PathBoxItem).Path;

            if (pointerRoutedEventArgs != null)
            {
                await Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () =>
                {
                    await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), itemTappedPath);
                });
                e.Handled = true;
                pointerRoutedEventArgs = null;
                return;
            }

            ToolbarPathItemInvoked?.Invoke(this, new PathNavigationEventArgs()
            {
                ItemPath = itemTappedPath
            });
        }

        public void SwitchSearchBoxVisibility()
        {
            if (IsSearchBoxVisible)
            {
                SearchBox.Query = string.Empty;
                IsSearchBoxVisible = false;
            }
            else
            {
                IsSearchBoxVisible = true;

                // Given that binding and layouting might take a few cycles, when calling UpdateLayout
                // we can guarantee that the focus call will be able to find an open ASB
                var searchbox = NavToolbar.FindDescendant("SearchRegion") as SearchBox;
                searchbox?.UpdateLayout();
                searchbox?.Focus(FocusState.Programmatic);
            }
        }

        public void UpdateAdditionnalActions()
        {
            OnPropertyChanged(nameof(HasAdditionnalAction));
            OnPropertyChanged(nameof(CanEmptyRecycleBin));
        }

        private NavigationToolbar NavToolbar => (Window.Current.Content as Frame).FindDescendant<NavigationToolbar>();

        #region WidgetsPage Widgets

        public bool ShowFoldersWidget
        {
            get => UserSettingsService.WidgetsSettingsService.ShowFoldersWidget;
            set
            {
                if (value != UserSettingsService.WidgetsSettingsService.ShowFoldersWidget)
                {
                    UserSettingsService.WidgetsSettingsService.ShowFoldersWidget = value;

                    RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowDrivesWidget
        {
            get => UserSettingsService.WidgetsSettingsService.ShowDrivesWidget;
            set
            {
                if (value != UserSettingsService.WidgetsSettingsService.ShowDrivesWidget)
                {
                    UserSettingsService.WidgetsSettingsService.ShowDrivesWidget = value;

                    RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowBundlesWidget
        {
            get => UserSettingsService.WidgetsSettingsService.ShowBundlesWidget;
            set
            {
                if (value != UserSettingsService.WidgetsSettingsService.ShowBundlesWidget)
                {
                    UserSettingsService.WidgetsSettingsService.ShowBundlesWidget = value;

                    RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowRecentFilesWidget
        {
            get => UserSettingsService.WidgetsSettingsService.ShowRecentFilesWidget;
            set
            {
                if (value != UserSettingsService.WidgetsSettingsService.ShowRecentFilesWidget)
                {
                    UserSettingsService.WidgetsSettingsService.ShowRecentFilesWidget = value;

                    RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        #endregion YourHome Widgets

        public void CloseSearchBox()
        {
            SearchBox.Query = string.Empty;
            IsSearchBoxVisible = false;
        }

        public void SearchRegion_LostFocus(object sender, RoutedEventArgs e)
        {
            var focusedElement = FocusManager.GetFocusedElement();
            if ((focusedElement is Button bttn && bttn.Name == "SearchButton") || focusedElement is FlyoutBase || focusedElement is AppBarButton)
            {
                return;
            }

            CloseSearchBox();
        }

        private void SearchRegion_Escaped(object sender, ISearchBox searchBox) => IsSearchBoxVisible = false;

        public ICommand SelectAllContentPageItemsCommand { get; set; }

        public ICommand InvertContentPageSelctionCommand { get; set; }

        public ICommand ClearContentPageSelectionCommand { get; set; }

        public ICommand PasteItemsFromClipboardCommand { get; set; }

        public ICommand OpenNewWindowCommand { get; set; }

        public ICommand OpenNewPaneCommand { get; set; }

        public ICommand ClosePaneCommand { get; set; }

        public ICommand OpenDirectoryInDefaultTerminalCommand { get; set; }

        public ICommand CreateNewFileCommand { get; set; }

        public ICommand CreateNewFolderCommand { get; set; }

        public ICommand CopyCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand Rename { get; set; }

        public ICommand Share { get; set; }

        public ICommand CutCommand { get; set; }

        public ICommand EmptyRecycleBinCommand { get; set; }

        public async Task SetPathBoxDropDownFlyoutAsync(MenuFlyout flyout, PathBoxItem pathItem, IShellPage shellPage)
        {
            var nextPathItemTitle = PathComponents[PathComponents.IndexOf(pathItem) + 1].Title;
            IList<StorageFolderWithPath> childFolders = null;

            StorageFolderWithPath folder = await shellPage.FilesystemViewModel.GetFolderWithPathFromPathAsync(pathItem.Path);
            if (folder != null)
            {
                childFolders = (await FilesystemTasks.Wrap(() => folder.GetFoldersWithPathAsync(string.Empty))).Result;
            }
            flyout.Items?.Clear();

            if (childFolders == null || childFolders.Count == 0)
            {
                var flyoutItem = new MenuFlyoutItem
                {
                    Icon = new FontIcon { Glyph = "\uE7BA" },
                    Text = "SubDirectoryAccessDenied".GetLocalized(),
                    //Foreground = (SolidColorBrush)Application.Current.Resources["SystemControlErrorTextForegroundBrush"],
                    FontSize = 12
                };
                flyout.Items.Add(flyoutItem);
                return;
            }

            var boldFontWeight = new FontWeight { Weight = 800 };
            var normalFontWeight = new FontWeight { Weight = 400 };

            var workingPath = PathComponents
                    [PathComponents.Count - 1].
                    Path?.TrimEnd(Path.DirectorySeparatorChar);

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

                flyout.Items.Add(flyoutItem);
            }
        }

        public async Task CheckPathInput(string currentInput, string currentSelectedPath, IShellPage shellPage)
        {
            currentInput = currentInput.Replace("\\\\", "\\", StringComparison.Ordinal);

            if (currentInput.StartsWith('\\') && !currentInput.StartsWith("\\\\", StringComparison.Ordinal))
            {
                currentInput = currentInput.Insert(0, "\\");
            }

            if (currentSelectedPath == currentInput || string.IsNullOrWhiteSpace(currentInput))
            {
                return;
            }

            if (currentInput != shellPage.FilesystemViewModel.WorkingDirectory || shellPage.CurrentPageType == typeof(WidgetsPage))
            {
                if (currentInput.Equals("Home".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                {
                    shellPage.NavigateHome();
                }
                else
                {
                    currentInput = StorageFileExtensions.GetPathWithoutEnvironmentVariable(currentInput);
                    if (currentSelectedPath == currentInput)
                    {
                        return;
                    }

                    var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(currentInput));

                    var resFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(currentInput, item));
                    if (resFolder || FolderHelpers.CheckFolderAccessWithWin32(currentInput))
                    {
                        var matchingDrive = App.DrivesManager.Drives.FirstOrDefault(x => PathNormalization.NormalizePath(currentInput).StartsWith(PathNormalization.NormalizePath(x.Path), StringComparison.Ordinal));
                        if (matchingDrive != null && matchingDrive.Type == DataModels.NavigationControlItems.DriveType.CDRom && matchingDrive.MaxSpace == ByteSizeLib.ByteSize.FromBytes(0))
                        {
                            bool ejectButton = await DialogDisplayHelper.ShowDialogAsync("InsertDiscDialog/Title".GetLocalized(), string.Format("InsertDiscDialog/Text".GetLocalized(), matchingDrive.Path), "InsertDiscDialog/OpenDriveButton".GetLocalized(), "Close".GetLocalized());
                            if (ejectButton)
                            {
                                await DriveHelpers.EjectDeviceAsync(matchingDrive.Path);
                            }
                            return;
                        }
                        var pathToNavigate = resFolder.Result?.Path ?? currentInput;
                        shellPage.NavigateToPath(pathToNavigate);
                    }
                    else if (FtpHelpers.IsFtpPath(currentInput))
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
                            var workingDir = string.IsNullOrEmpty(shellPage.FilesystemViewModel.WorkingDirectory)
                                    || shellPage.CurrentPageType == typeof(WidgetsPage)
                                ? CommonPaths.HomePath
                                : shellPage.FilesystemViewModel.WorkingDirectory;

                            // Launch terminal application if possible
                            foreach (var terminal in App.TerminalController.Model.Terminals)
                            {
                                if (terminal.Path.Equals(currentInput, StringComparison.OrdinalIgnoreCase)
                                    || terminal.Path.Equals(currentInput + ".exe", StringComparison.OrdinalIgnoreCase) || terminal.Name.Equals(currentInput, StringComparison.OrdinalIgnoreCase))
                                {
                                    var connection = await AppServiceConnectionHelper.Instance;
                                    if (connection != null)
                                    {
                                        var value = new ValueSet()
                                        {
                                            { "Arguments", "LaunchApp" },
                                            { "WorkingDirectory", workingDir },
                                            { "Application", terminal.Path },
                                            { "Parameters", string.Format(terminal.Arguments, workingDir) }
                                        };
                                        await connection.SendMessageAsync(value);
                                    }
                                    return;
                                }
                            }

                            try
                            {
                                if (!await Launcher.LaunchUriAsync(new Uri(currentInput)))
                                {
                                    await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalized(),
                                        string.Format("InvalidItemDialogContent".GetLocalized(), Environment.NewLine, resFolder.ErrorCode.ToString()));
                                }
                            }
                            catch (Exception ex) when (ex is UriFormatException || ex is ArgumentException)
                            {
                                await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalized(),
                                    string.Format("InvalidItemDialogContent".GetLocalized(), Environment.NewLine, resFolder.ErrorCode.ToString()));
                            }
                        }
                    }
                }

                PathControlDisplayText = shellPage.FilesystemViewModel.WorkingDirectory;
            }
        }

        public async void SetAddressBarSuggestions(AutoSuggestBox sender, IShellPage shellpage, int maxSuggestions = 7)
        {
            if (!string.IsNullOrWhiteSpace(sender.Text) && shellpage.FilesystemViewModel != null)
            {
                try
                {
                    IList<ListedItem> suggestions = null;
                    var expandedPath = StorageFileExtensions.GetPathWithoutEnvironmentVariable(sender.Text);
                    var folderPath = PathNormalization.GetParentDir(expandedPath) ?? expandedPath;
                    var folder = await shellpage.FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);
                    var currPath = await folder.Result.GetFoldersWithPathAsync(Path.GetFileName(expandedPath), (uint)maxSuggestions);
                    if (currPath.Count >= maxSuggestions)
                    {
                        suggestions = currPath.Select(x => new ListedItem(null)
                        {
                            ItemPath = x.Path,
                            ItemNameRaw = x.Folder.DisplayName
                        }).ToList();
                    }
                    else if (currPath.Any())
                    {
                        var subPath = await currPath.First().GetFoldersWithPathAsync((uint)(maxSuggestions - currPath.Count));
                        suggestions = currPath.Select(x => new ListedItem(null)
                        {
                            ItemPath = x.Path,
                            ItemNameRaw = x.Folder.DisplayName
                        }).Concat(
                            subPath.Select(x => new ListedItem(null)
                            {
                                ItemPath = x.Path,
                                ItemNameRaw = PathNormalization.Combine(currPath.First().Folder.DisplayName, x.Folder.DisplayName)
                            })).ToList();
                    }
                    else
                    {
                        suggestions = new List<ListedItem>() { new ListedItem(null) {
                        ItemPath = shellpage.FilesystemViewModel.WorkingDirectory,
                        ItemNameRaw = "NavigationToolbarVisiblePathNoResults".GetLocalized() } };
                    }

                    // NavigationBarSuggestions becoming empty causes flickering of the suggestion box
                    // Here we check whether at least an element is in common between old and new list
                    if (!NavigationBarSuggestions.IntersectBy(suggestions, x => x.ItemName).Any())
                    {
                        // No elements in common, update the list in-place
                        for (int si = 0; si < suggestions.Count; si++)
                        {
                            if (si < NavigationBarSuggestions.Count)
                            {
                                NavigationBarSuggestions[si].ItemNameRaw = suggestions[si].ItemNameRaw;
                                NavigationBarSuggestions[si].ItemPath = suggestions[si].ItemPath;
                            }
                            else
                            {
                                NavigationBarSuggestions.Add(suggestions[si]);
                            }
                        }
                        while (NavigationBarSuggestions.Count > suggestions.Count)
                        {
                            NavigationBarSuggestions.RemoveAt(NavigationBarSuggestions.Count - 1);
                        }
                    }
                    else
                    {
                        // At least an element in common, show animation
                        foreach (var s in NavigationBarSuggestions.ExceptBy(suggestions, x => x.ItemNameRaw).ToList())
                        {
                            NavigationBarSuggestions.Remove(s);
                        }
                        foreach (var s in suggestions.ExceptBy(NavigationBarSuggestions, x => x.ItemNameRaw).ToList())
                        {
                            NavigationBarSuggestions.Insert(suggestions.IndexOf(s), s);
                        }
                    }
                }
                catch
                {
                    NavigationBarSuggestions.Clear();
                    NavigationBarSuggestions.Add(new ListedItem(null)
                    {
                        ItemPath = shellpage.FilesystemViewModel.WorkingDirectory,
                        ItemNameRaw = "NavigationToolbarVisiblePathNoResults".GetLocalized()
                    });
                }
            }
        }

        private bool hasItem = true;
        public bool HasItem
        {
            get => hasItem;
            set
            {
                if (SetProperty(ref hasItem, value))
                {
                    OnPropertyChanged(nameof(CanEmptyRecycleBin));
                }
            }

        }

        private List<ListedItem> selectedItems;

        public List<ListedItem> SelectedItems
        {
            get => selectedItems;
            set
            {
                if (SetProperty(ref selectedItems, value))
                {
                    OnPropertyChanged(nameof(CanCopy));
                    OnPropertyChanged(nameof(CanShare));
                    OnPropertyChanged(nameof(CanRename));
                }
            }
        }

        public bool HasAdditionnalAction => InstanceViewModel.IsPageTypeRecycleBin;

        public bool CanCopy => SelectedItems is not null && SelectedItems.Any();
        public bool CanShare => SelectedItems is not null && SelectedItems.Any() && DataTransferManager.IsSupported() && !SelectedItems.Any(x => (x.IsShortcutItem && !x.IsLinkItem) || x.IsHiddenItem || (x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsZipItem));
        public bool CanRename => SelectedItems is not null && SelectedItems.Count == 1;
        public bool CanEmptyRecycleBin => InstanceViewModel.IsPageTypeRecycleBin && HasItem;

        public void Dispose()
        {
            SearchBox.Escaped -= SearchRegion_Escaped;
            UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;

            InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
            InstanceViewModel.FolderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
        }
    }
}