// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IAddressToolbarViewModel
	{
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


		public event ItemDraggedOverPathItemEventHandler ItemDraggedOverPathItem;

		public event EventHandler RefreshWidgetsRequested;
	}
}
