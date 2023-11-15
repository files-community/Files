// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.TabBar;

namespace Files.App.Views.Shells
{
	public interface IShellPage : ITabBarItemContent, IMultiPaneInfo, IDisposable, INotifyPropertyChanged
	{
		ItemViewModel FilesystemViewModel { get; }

		CurrentInstanceViewModel InstanceViewModel { get; }

		StorageHistoryHelpers StorageHistoryHelpers { get; }

		IBaseLayoutPage SlimContentPage { get; }

		Type CurrentPageType { get; }

		IFilesystemHelpers FilesystemHelpers { get; }

		ToolbarViewModel ToolbarViewModel { get; }

		bool CanNavigateBackward { get; }

		bool CanNavigateForward { get; }

		/// <summary>
		/// True if the pane that contains this page is current.
		/// </summary>
		bool IsCurrentPane { get; }

		/// <summary>
		/// Returns a <see cref="Task"/> to wait until the pane and column become current.
		/// </summary>
		/// <returns>A <see cref="Task"/> to wait until the pane and column become current.</returns>
		Task WhenIsCurrent();

		Task RefreshIfNoWatcherExistsAsync();

		Task Refresh_Click();

		void Back_Click();

		void Forward_Click();

		void Up_Click();

		void UpdatePathUIToWorkingDirectory(string newWorkingDir, string singleItemOverride = null);

		void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null);

		/// <summary>
		/// Gets the layout mode for the specified path then navigates to it
		/// </summary>
		/// <param name="navigationPath"></param>
		/// <param name="navArgs"></param>
		public void NavigateToPath(string navigationPath, NavigationArguments navArgs = null);

		/// <summary>
		/// Navigates to the home page
		/// </summary>
		public void NavigateHome();

		void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs);

		void RemoveLastPageFromBackStack();

		/// <summary>
		/// Replaces any outdated entries with those of the correct page type
		/// </summary>
		void ResetNavigationStackLayoutMode();

		void SubmitSearch(string query, bool searchUnindexedItems);

		/// <summary>
		/// Used to make commands in the column view work properly
		/// </summary>
		public bool IsColumnView { get; }
	}

	public interface IPaneHolder : IDisposable, INotifyPropertyChanged
	{
		public IShellPage ActivePane { get; set; }

		// If column view, returns the last column shell page, otherwise returns the active pane normally
		public IShellPage ActivePaneOrColumn { get; }

		public IFilesystemHelpers FilesystemHelpers { get; }

		public CustomTabViewItemParameter TabItemParameter { get; set; }

		public void OpenPathInNewPane(string path);

		public void CloseActivePane();

		public bool IsLeftPaneActive { get; }

		public bool IsRightPaneActive { get; }

		// Another pane is shown
		public bool IsMultiPaneActive { get; }

		// Multi pane is enabled
		public bool IsMultiPaneEnabled { get; }
	}

	public interface IMultiPaneInfo
	{
		public IPaneHolder PaneHolder { get; }
	}
}
