// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.UserControls
{
	public interface IAddressToolbar
	{
		public bool IsSearchBoxVisible { get; set; }

		public bool IsEditModeEnabled { get; set; }

		public bool IsCommandPaletteOpen { get; set; }

		public bool CanRefresh { get; set; }

		public bool CanCopyPathInPage { get; set; }

		public bool CanNavigateToParent { get; set; }

		public bool CanGoBack { get; set; }

		public bool CanGoForward { get; set; }

		public string PathControlDisplayText { get; set; }

		public delegate void ToolbarQuerySubmittedEventHandler(object sender, ToolbarQuerySubmittedEventArgs e);

		public event ToolbarQuerySubmittedEventHandler PathBoxQuerySubmitted;

		public event EventHandler EditModeEnabled;

		public event EventHandler RefreshRequested;

		public event EventHandler RefreshWidgetsRequested;

		public void SwitchSearchBoxVisibility();

		public ISearchBox SearchBox { get; }
	}
}
