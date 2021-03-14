using Files.DataModels;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using static Files.UserControls.INavigationToolbar;

namespace Files.UserControls
{
    public sealed partial class ExpandRibbonToolbar : UserControl, INavigationToolbar, INotifyPropertyChanged
    {
        public delegate void ToolbarPathItemInvokedEventHandler(object sender, PathNavigationEventArgs e);

        public delegate void ToolbarFlyoutOpenedEventHandler(object sender, ToolbarFlyoutOpenedEventArgs e);

        public delegate void ToolbarPathItemLoadedEventHandler(object sender, ToolbarPathItemLoadedEventArgs e);

        public delegate void AddressBarTextEnteredEventHandler(object sender, AddressBarTextEnteredEventArgs e);

        public delegate void PathBoxItemDroppedEventHandler(object sender, PathBoxItemDroppedEventArgs e);

        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> SearchQuerySubmitted;

        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> SearchTextChanged;

        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SearchSuggestionChosen;

        public event ToolbarPathItemInvokedEventHandler ToolbarPathItemInvoked;

        public event ToolbarFlyoutOpenedEventHandler ToolbarFlyoutOpened;

        public event ToolbarPathItemLoadedEventHandler ToolbarPathItemLoaded;

        public event PropertyChangedEventHandler PropertyChanged;

        public event ItemDraggedOverPathItemEventHandler ItemDraggedOverPathItem;

        public event EventHandler EditModeEnabled;

        public event ToolbarQuerySubmittedEventHandler PathBoxQuerySubmitted;

        public event AddressBarTextEnteredEventHandler AddressBarTextEntered;

        public event PathBoxItemDroppedEventHandler PathBoxItemDropped;

        public event EventHandler BackRequested;

        public event EventHandler ForwardRequested;

        public event EventHandler UpRequested;

        public event EventHandler RefreshRequested;


        public static readonly DependencyProperty CanCreateFileInPageProperty = DependencyProperty.Register(
          "CanCreateFileInPage",
          typeof(bool),
          typeof(ExpandRibbonToolbar),
          new PropertyMetadata(null)
        );

        public bool CanCreateFileInPage
        {
            get
            {
                return (bool)GetValue(CanCreateFileInPageProperty);
            }
            set
            {
                SetValue(CanCreateFileInPageProperty, value);
            }
        }

        public static readonly DependencyProperty CanCopyPathInPageProperty = DependencyProperty.Register(
          "CanCopyPathInPage",
          typeof(bool),
          typeof(ExpandRibbonToolbar),
          new PropertyMetadata(null)
        );

        public bool CanCopyPathInPage
        {
            get
            {
                return (bool)GetValue(CanCopyPathInPageProperty);
            }
            set
            {
                SetValue(CanCopyPathInPageProperty, value);
            }
        }

        private List<ShellNewEntry> cachedNewContextMenuEntries { get; set; }

        public ExpandRibbonToolbar()
        {
            this.InitializeComponent();
            this.Loading += NavigationToolbar_Loading;
        }

        private async void NavigationToolbar_Loading(FrameworkElement sender, object args)
        {
            cachedNewContextMenuEntries = await RegistryHelper.GetNewContextMenuEntries();
        }

        private bool manualEntryBoxLoaded = false;

        public bool ManualEntryBoxLoaded
        {
            get
            {
                return manualEntryBoxLoaded;
            }
            set
            {
                if (value != manualEntryBoxLoaded)
                {
                    manualEntryBoxLoaded = value;
                    NotifyPropertyChanged(nameof(ManualEntryBoxLoaded));
                }
            }
        }

        private bool clickablePathLoaded = true;

        public bool ClickablePathLoaded
        {
            get
            {
                return clickablePathLoaded;
            }
            set
            {
                if (value != clickablePathLoaded)
                {
                    clickablePathLoaded = value;
                    NotifyPropertyChanged(nameof(ClickablePathLoaded));
                }
            }
        }

        private bool showMultiPaneControls;

        public bool ShowMultiPaneControls
        {
            get
            {
                return showMultiPaneControls;
            }
            set
            {
                if (value != showMultiPaneControls)
                {
                    showMultiPaneControls = value;
                    NotifyPropertyChanged(nameof(ShowMultiPaneControls));
                }
            }
        }

        private bool isMultiPaneActive;

        public bool IsMultiPaneActive
        {
            get
            {
                return isMultiPaneActive;
            }
            set
            {
                if (value != isMultiPaneActive)
                {
                    isMultiPaneActive = value;
                    NotifyPropertyChanged(nameof(IsMultiPaneActive));
                    NotifyPropertyChanged(nameof(IsPageSecondaryPane));
                }
            }
        }

        public bool IsPageSecondaryPane => !IsMultiPaneActive || !IsPageMainPane;

        private bool isPageMainPane;

        public bool IsPageMainPane
        {
            get
            {
                return isPageMainPane;
            }
            set
            {
                if (value != isPageMainPane)
                {
                    isPageMainPane = value;
                    NotifyPropertyChanged(nameof(IsPageMainPane));
                    NotifyPropertyChanged(nameof(IsPageSecondaryPane));
                }
            }
        }

        private bool areKeyboardAcceleratorsEnabled;

        public bool AreKeyboardAcceleratorsEnabled
        {
            get
            {
                return areKeyboardAcceleratorsEnabled;
            }
            set
            {
                if (value != areKeyboardAcceleratorsEnabled)
                {
                    areKeyboardAcceleratorsEnabled = value;
                    NotifyPropertyChanged(nameof(AreKeyboardAcceleratorsEnabled));
                }
            }
        }

        public string PathText { get; set; }

        private bool isSearchRegionVisible;

        public bool IsSearchRegionVisible
        {
            get
            {
                return isSearchRegionVisible;
            }
            set
            {
                if (value != isSearchRegionVisible)
                {
                    isSearchRegionVisible = value;
                    NotifyPropertyChanged(nameof(IsSearchRegionVisible));
                }
            }
        }

        bool INavigationToolbar.IsEditModeEnabled
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
                    //Interaction.FindChild<TextBox>(VisiblePath)?.SelectAll();
                }
                else
                {
                    ManualEntryBoxLoaded = false;
                    ClickablePathLoaded = true;
                }
            }
        }

        public ObservableCollection<ListedItem> NavigationBarSuggestions = new ObservableCollection<ListedItem>();

        string INavigationToolbar.PathControlDisplayText
        {
            get
            {
                return PathText;
            }
            set
            {
                PathText = value;
                NotifyPropertyChanged(nameof(PathText));
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<PathBoxItem> PathComponents { get; } = new ObservableCollection<PathBoxItem>();
        public bool CanRefresh { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CanNavigateToParent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CanGoBack { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CanGoForward { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private string dragOverPath = null;
        private DispatcherTimer dragOverTimer = new DispatcherTimer();

        private void PathBoxItem_DragLeave(object sender, DragEventArgs e)
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

        private async void PathBoxItem_DragOver(object sender, DragEventArgs e)
        {
            if (!((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
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
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
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

        private void PathBoxItem_Drop(object sender, DragEventArgs e)
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

        private void PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var itemTappedPath = ((sender as TextBlock).DataContext as PathBoxItem).Path;
            ToolbarPathItemInvoked?.Invoke(this, new PathNavigationEventArgs()
            {
                ItemPath = itemTappedPath
            });
        }

        private void PathItemSeparator_Loaded(object sender, RoutedEventArgs e)
        {
            var pathSeparatorIcon = sender as FontIcon;
            pathSeparatorIcon.Tapped += (s, e) => pathSeparatorIcon.ContextFlyout.ShowAt(pathSeparatorIcon);
            pathSeparatorIcon.ContextFlyout.Opened += (s, e) => { pathSeparatorIcon.Glyph = "\uE70D"; };
            pathSeparatorIcon.ContextFlyout.Closed += (s, e) => { pathSeparatorIcon.Glyph = "\uE76C"; };
        }

        private void PathItemSeparator_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
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

        private void PathboxItemFlyout_Opened(object sender, object e)
        {
            ToolbarFlyoutOpened?.Invoke(this, new ToolbarFlyoutOpenedEventArgs() { OpenedFlyout = sender as MenuFlyout });
        }

        public void ClearSearchBoxQueryText(bool collapseSearchRegion = false)
        {

        }

        private void Pin_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MoveTo_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {

        }

        private void MoveTo_IsCheckedChanged(ToggleSplitButton sender, ToggleSplitButtonIsCheckedChangedEventArgs args)
        {

        }

        private void MoveToFoldersView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CopyTo_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {

        }

        private void CopyTo_IsCheckedChanged(ToggleSplitButton sender, ToggleSplitButtonIsCheckedChangedEventArgs args)
        {

        }

        private void CopyToFoldersView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Delete_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {

        }

        private void Delete_IsCheckedChanged(ToggleSplitButton sender, ToggleSplitButtonIsCheckedChangedEventArgs args)
        {

        }

        private void DeleteView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NewFolder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Select_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {

        }

        private void Select_IsCheckedChanged(ToggleSplitButton sender, ToggleSplitButtonIsCheckedChangedEventArgs args)
        {

        }

        private void SelectView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}