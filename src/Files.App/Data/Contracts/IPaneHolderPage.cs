// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents contracts for pane folder pane.
	/// </summary>
	public interface IPaneHolderPage : IDisposable, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets the active pane.
		/// </summary>
		public IShellPage ActivePane { get; set; }

		/// <summary>
		/// Gets the active pane or column.
		/// If the layout type is Columns, returns the last column shell page.
		/// </summary>
		public IShellPage ActivePaneOrColumn { get; }

		/// <summary>
		/// Gets the service for file system operations.
		/// </summary>
		public IFilesystemHelpers FilesystemHelpers { get; }

		/// <summary>
		/// Gets the parameter specified in the selected <see cref="TabBarItem"/>.
		/// </summary>
		public TabBarItemParameter TabBarItemParameter { get; set; }

		/// <summary>
		/// Opens path in a new pane.
		/// </summary>
		/// <param name="path">Path to open in a new pane.</param>
		public void OpenPathInNewPane(string path);

		/// <summary>
		/// Closes the active pane.
		/// </summary>
		public void CloseActivePane();

		/// <summary>
		/// Gets the value that indicates whether the left pane is active.
		/// </summary>
		public bool IsLeftPaneActive { get; }

		/// <summary>
		/// Gets the value that indicates whether the right pane is active.
		/// </summary>
		public bool IsRightPaneActive { get; }

		/// <summary>
		/// Gets the value that indicates whether multiple pane is active.
		/// </summary>
		/// <remarks>
		/// Reserved for future use.
		/// </remarks>
		public bool IsMultiPaneActive { get; }

		/// <summary>
		/// Gets the value that indicates whether multiple pane is enabled.
		/// </summary>
		/// <remarks>
		/// Reserved for future use.
		/// </remarks>
		public bool IsMultiPaneEnabled { get; }
	}
}
