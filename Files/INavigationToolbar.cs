using Files.Views.Pages;
using System;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public interface INavigationToolbar
    {
        public bool IsSearchReigonVisible { get; set; }
        public bool IsEditModeEnabled { get; set; }
        public bool CanRefresh { get; set; }
        public bool CanNavigateToParent { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public string PathControlDisplayText { get; set; }
        public ObservableCollection<PathBoxItem> PathComponents { get; }

        public delegate void ToolbarQuerySubmittedEventHandler(object sender, ToolbarQuerySubmittedEventArgs e);

        public delegate void ItemDraggedOverPathItemEventHandler(object sender, PathNavigationEventArgs e);

        public event ToolbarQuerySubmittedEventHandler QuerySubmitted;

        public event EventHandler EditModeEnabled;

        public event ItemDraggedOverPathItemEventHandler ItemDraggedOverPathItem;
    }

    public class ToolbarQuerySubmittedEventArgs
    {
        public string QueryText { get; set; } = null;
    }

    public class PathNavigationEventArgs
    {
        public Type LayoutType { get; set; }
        public string ItemPath { get; set; }
    }

    public class ToolbarFlyoutOpenedEventArgs
    {
        public MenuFlyout OpenedFlyout { get; set; }
    }

    public class ToolbarPathItemLoadedEventArgs
    {
        public MenuFlyout OpenedFlyout { get; set; }
        public PathBoxItem Item { get; set; }
    }

    public class AddressBarTextEnteredEventArgs
    {
        public AutoSuggestBox AddressBarTextField { get; set; }
    }

    public class PathBoxItemDroppedEventArgs
    {
        public DataPackageView Package { get; set; }
        public string Path { get; set; }
        public DataPackageOperation AcceptedOperation { get; set; }
    }
}