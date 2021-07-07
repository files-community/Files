﻿using Files.Views;
using System;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public interface INavigationToolbar
    {
        public bool IsSearchBoxVisible { get; set; }
        public bool IsEditModeEnabled { get; set; }
        public bool CanRefresh { get; set; }
        public bool CanCopyPathInPage { get; set; }
        public bool CanNavigateToParent { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public bool IsSingleItemOverride { get; set; }
        public string PathControlDisplayText { get; set; }
        public ObservableCollection<PathBoxItem> PathComponents { get; }

        public delegate void ToolbarQuerySubmittedEventHandler(object sender, ToolbarQuerySubmittedEventArgs e);

        public delegate void ItemDraggedOverPathItemEventHandler(object sender, PathNavigationEventArgs e);

        public event ToolbarQuerySubmittedEventHandler PathBoxQuerySubmitted;

        public event EventHandler EditModeEnabled;

        public event ItemDraggedOverPathItemEventHandler ItemDraggedOverPathItem;

        public event EventHandler BackRequested;

        public event EventHandler ForwardRequested;

        public event EventHandler UpRequested;

        public event EventHandler RefreshRequested;

        public event EventHandler RefreshWidgetsRequested;

        public void SwitchSearchBoxVisibility();

        public ISearchBox SearchBox { get; }
    }

    public class ToolbarQuerySubmittedEventArgs
    {
        public string QueryText { get; set; } = null;
    }

    public class PathNavigationEventArgs
    {
        public string ItemPath { get; set; }
        public string ItemName { get; set; }
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