// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents interface of <see cref="ShellPanesPage"/>.
	/// </summary>
	public interface IShellPanesPage : IDisposable, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the current focused shell pane.
		/// </summary>
		public IShellPage? ActivePane { get; }

		/// <summary>
		/// Gets the current focused shell pane, or the last column when the focused shell pane layout type is <see cref="ColumnsLayoutPage"/>.
		/// </summary>
		public IShellPage? ActivePaneOrColumn { get; }

		// TODO: Remove this our of this class
		public IFilesystemHelpers? FilesystemHelpers { get; }

		// TODO: Remove this our of this class
		public TabBarItemParameter? TabBarItemParameter { get; set; }

		/// <summary>
		/// Adds a new pane with path and pane addition direction if needed.
		/// </summary>
		/// <param name="path">The path to open in the new pane.</param>
		/// <param name="direction">The alignment direction.</param>
		public void OpenSecondaryPane(string path = "", ShellPaneAlignmentDirection direction = ShellPaneAlignmentDirection.Horizontal)

		public void CloseActivePane();

		public void FocusOtherPane();

		public bool IsLeftPaneActive { get; }

		public bool IsRightPaneActive { get; }

		public bool IsMultiPaneActive { get; }

		public bool CanBeDualPane { get; }
	}
}
