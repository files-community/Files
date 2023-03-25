using Files.App.Filesystem;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels;
using Files.App.Views;
using System;
using System.ComponentModel;

namespace Files.App
{
	public interface IShellPage : ITabItemContent, IMultiPaneInfo, IDisposable, INotifyPropertyChanged
	{
		ItemViewModel FilesystemViewModel { get; }

		CurrentInstanceViewModel InstanceViewModel { get; }

		IBaseLayout SlimContentPage { get; }

		Type CurrentPageType { get; }

		IFilesystemHelpers FilesystemHelpers { get; }

		ToolbarViewModel ToolbarViewModel { get; }

		bool CanNavigateBackward { get; }

		bool CanNavigateForward { get; }

		void Refresh_Click();

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

		public TabItemArguments TabItemArguments { get; set; }

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
		// The instance is the left (or only) pane
		public bool IsPageMainPane { get; }

		public IPaneHolder PaneHolder { get; }
	}
}