using Files.Filesystem;
using Files.UserControls;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using static Files.UserControls.INavigationToolbar;

namespace Files.ViewModels
{
    public class NavToolbarViewModel : ObservableObject, INavigationToolbar
    {
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
            set => SetProperty(ref pathText, value);
        }

        public ObservableCollection<ListedItem> NavigationBarSuggestions = new ObservableCollection<ListedItem>();


        public NavToolbarViewModel()
        {
            dragOverTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            SearchBox.SuggestionChosen += SearchRegion_SuggestionChosen;
            SearchBox.Escaped += SearchRegion_Escaped;
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

        public void PathBoxItem_DragLeave(object sender, DragEventArgs e)
        {
            if (!((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home" || pathBoxItem.Path == "NewTab".GetLocalized())
            {
                return;
            }

            if (pathBoxItem.Path == dragOverPath)
            {
                // Reset dragged over pathbox item
                dragOverPath = null;
            }
        }

        public void PathBoxItem_Drop(object sender, DragEventArgs e)
        {
            dragOverPath = null; // Reset dragged over pathbox item

            if (!((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home" || pathBoxItem.Path == "NewTab".GetLocalized())
            {
                return;
            }

            var deferral = e.GetDeferral();
            PathBoxItemDropped?.Invoke(this, new PathBoxItemDroppedEventArgs()
            {
                AcceptedOperation = e.AcceptedOperation,
                Package = e.DataView,
                Path = pathBoxItem.Path
            });
            deferral.Complete();
        }


        public async void PathBoxItem_DragOver(object sender, DragEventArgs e)
        {
            if (IsSingleItemOverride || !((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home" || pathBoxItem.Path == "NewTab".GetLocalized())
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

            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }

            e.Handled = true;
            var deferral = e.GetDeferral();

            IReadOnlyList<IStorageItem> storageItems;
            try
            {
                storageItems = await e.DataView.GetStorageItemsAsync();
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80040064)
            {
                e.AcceptedOperation = DataPackageOperation.None;
                deferral.Complete();
                return;
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, ex.Message);
                e.AcceptedOperation = DataPackageOperation.None;
                deferral.Complete();
                return;
            }

            if (!storageItems.Any(storageItem =>
            storageItem.Path.Replace(pathBoxItem.Path, string.Empty).
            Trim(Path.DirectorySeparatorChar).
            Contains(Path.DirectorySeparatorChar)))
            {
                e.AcceptedOperation = DataPackageOperation.None;
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
                    EditModeEnabled?.Invoke(this, new EventArgs());
                    //VisiblePath.Focus(FocusState.Programmatic);
                    //DependencyObjectHelpers.FindChild<TextBox>(VisiblePath)?.SelectAll();
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

        private bool clickablePathLoaded;
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

        public void PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var itemTappedPath = ((sender as TextBlock).DataContext as PathBoxItem).Path;
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
                //SearchRegion.UpdateLayout();

                //SearchRegion.Focus(FocusState.Programmatic); // TODO: Repimplement
            }
        }

        #region YourHome Widgets

        public bool ShowLibraryCardsWidget
        {
            get => App.AppSettings.ShowLibraryCardsWidget;
            set
            {
                if (App.AppSettings.ShowLibraryCardsWidget != value)
                {
                    App.AppSettings.ShowLibraryCardsWidget = value;

                    RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowDrivesWidget
        {
            get => App.AppSettings.ShowDrivesWidget;
            set
            {
                if (App.AppSettings.ShowDrivesWidget != value)
                {
                    App.AppSettings.ShowDrivesWidget = value;

                    RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowBundlesWidget
        {
            get => App.AppSettings.ShowBundlesWidget;
            set
            {
                if (App.AppSettings.ShowBundlesWidget != value)
                {
                    App.AppSettings.ShowBundlesWidget = value;

                    RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowRecentFilesWidget
        {
            get => App.AppSettings.ShowRecentFilesWidget;
            set
            {
                if (App.AppSettings.ShowRecentFilesWidget != value)
                {
                    App.AppSettings.ShowRecentFilesWidget = value;

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

        private void SearchRegion_SuggestionChosen(ISearchBox sender, SearchBoxSuggestionChosenEventArgs args) => IsSearchBoxVisible = false;
        private void SearchRegion_Escaped(object sender, ISearchBox searchBox) => IsSearchBoxVisible = false;
    }
}
