// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents contract class for Shell page.
	/// </summary>
	public interface IShellPage : ITabBarItemContent, IMultiPaneInfo, IDisposable, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets view model of <see cref="IShellPage"/>.
		/// </summary>
		ShellViewModel ShellViewModel { get; }

		/// <summary>
		/// Gets view model for current instance.
		/// </summary>
		ShellInstanceViewModel ShellInstanceViewModel { get; }

		/// <summary>
		/// Gets <see cref="StorageHistoryHelpers"/> instance.
		/// </summary>
		StorageHistoryHelpers StorageHistoryHelpers { get; }

		/// <summary>
		/// Gets contract for layout pages.
		/// </summary>
		IBaseLayoutPage SlimContentPage { get; }

		/// <summary>
		/// Gets type of content page.
		/// </summary>
		Type CurrentPageType { get; }

		/// <summary>
		/// Gets contract for storage helpers.
		/// </summary>
		IFilesystemHelpers FilesystemHelpers { get; }

		/// <summary>
		/// Gets view model of <see cref="AddressToolbar"/>
		/// </summary>
		ToolbarViewModel ToolbarViewModel { get; }

		/// <summary>
		/// Gets the value that indicates whether the content page can navigate backward.
		/// </summary>
		bool CanNavigateBackward { get; }

		/// <summary>
		/// Gets the value that indicates whether the content page can navigate forward.
		/// </summary>
		bool CanNavigateForward { get; }

		/// <summary>
		/// True if the pane that contains this page is current.
		/// </summary>
		bool IsCurrentPane { get; }

		/// <summary>
		/// Gets the value whether the page type is columns layout.
		/// </summary>
		public bool IsColumnView { get; }

		/// <summary>
		/// Returns a <see cref="Task"/> to wait until the pane and column become current.
		/// </summary>
		Task WhenIsCurrent();

		Task RefreshIfNoWatcherExistsAsync();

		Task Refresh_Click();

		void Back_Click();

		void Forward_Click();

		void Up_Click();

		Task UpdatePathUIToWorkingDirectoryAsync(string newWorkingDir, string singleItemOverride = null);

		void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null);

		/// <summary>
		/// Gets the layout mode for the specified path then navigates to it
		/// </summary>
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

		void SubmitSearch(string query);
	}
}
