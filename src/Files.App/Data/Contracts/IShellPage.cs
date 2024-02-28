// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents contract for the shell page.
	/// </summary>
	public interface IShellPage : ITabBarItemContent, IMultiPane, IDisposable, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the view model for the shell page.
		/// </summary>
		ShellViewModel FilesystemViewModel { get; }

		// TODO: Remove
		CurrentInstanceViewModel InstanceViewModel { get; }

		StorageHistoryHelpers StorageHistoryHelpers { get; }

		IBaseLayoutPage SlimContentPage { get; }

		Type CurrentPageType { get; }

		IFilesystemHelpers FilesystemHelpers { get; }

		AddressToolbarViewModel ToolbarViewModel { get; }

		bool CanNavigateBackward { get; }

		bool CanNavigateForward { get; }

		/// <summary>
		/// Gets the value that indicates whether the pane that contains this pane is selected.
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

		Task UpdatePathUIToWorkingDirectoryAsync(string newWorkingDir, string singleItemOverride = null);

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

		void SubmitSearch(string query);

		/// <summary>
		/// Used to make commands in the column view work properly
		/// </summary>
		public bool IsColumnView { get; }
	}
}
