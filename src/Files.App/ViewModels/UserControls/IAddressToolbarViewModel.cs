// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Views;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.ViewModels.UserControls
{
	public interface IAddressToolbar
	{
		public bool IsSearchBoxVisible { get; set; }

		public bool IsEditModeEnabled { get; set; }

		/// <summary>
		/// Boolean to determine if the command palette is open
		/// </summary>
		public bool IsCommandPaletteOpen { get; set; }

		public bool CanRefresh { get; set; }

		public bool CanCopyPathInPage { get; set; }

		public bool CanNavigateToParent { get; set; }

		public bool CanGoBack { get; set; }

		public bool CanGoForward { get; set; }

		public string PathControlDisplayText { get; set; }

		public delegate void ToolbarQuerySubmittedEventHandler(object sender, ToolbarQuerySubmittedEventArgs e);

		public delegate void ItemDraggedOverPathItemEventHandler(object sender, PathNavigationEventArgs e);

		public event ToolbarQuerySubmittedEventHandler PathBoxQuerySubmitted;

		public event EventHandler EditModeEnabled;

		public event EventHandler RefreshRequested;

		public event EventHandler RefreshWidgetsRequested;

		public void SwitchSearchBoxVisibility();

		public ISearchBox SearchBox { get; }
	}
}
