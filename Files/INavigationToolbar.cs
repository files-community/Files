using Files.Views;
using System;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public interface INavigationToolbar
    {
        public delegate void ItemDraggedOverPathItemEventHandler(object sender, PathNavigationEventArgs e);

        public delegate void ToolbarQuerySubmittedEventHandler(object sender, ToolbarQuerySubmittedEventArgs e);

        public event EventHandler BackRequested;

        public event EventHandler EditModeEnabled;

        public event EventHandler ForwardRequested;

        public event ItemDraggedOverPathItemEventHandler ItemDraggedOverPathItem;

        public event ToolbarQuerySubmittedEventHandler PathBoxQuerySubmitted;

        public event EventHandler RefreshRequested;

        public event EventHandler RefreshWidgetsRequested;

        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> SearchQuerySubmitted;

        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SearchSuggestionChosen;

        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> SearchTextChanged;

        public event EventHandler UpRequested;

        public bool CanCopyPathInPage { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public bool CanNavigateToParent { get; set; }
        public bool CanRefresh { get; set; }
        public bool IsEditModeEnabled { get; set; }
        public bool IsSearchRegionVisible { get; set; }
        public ObservableCollection<PathBoxItem> PathComponents { get; }
        public string PathControlDisplayText { get; set; }

        public void ClearSearchBoxQueryText(bool collapseSearchReigon = false);
    }

    public class AddressBarTextEnteredEventArgs
    {
        public AutoSuggestBox AddressBarTextField { get; set; }
    }

    public class PathBoxItemDroppedEventArgs
    {
        public DataPackageOperation AcceptedOperation { get; set; }
        public DataPackageView Package { get; set; }
        public string Path { get; set; }
    }

    public class PathNavigationEventArgs
    {
        public string ItemPath { get; set; }
    }

    public class ToolbarFlyoutOpenedEventArgs
    {
        public MenuFlyout OpenedFlyout { get; set; }
    }

    public class ToolbarPathItemLoadedEventArgs
    {
        public PathBoxItem Item { get; set; }
        public MenuFlyout OpenedFlyout { get; set; }
    }

    public class ToolbarQuerySubmittedEventArgs
    {
        public string QueryText { get; set; } = null;
    }
}