// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents contract class for pane holder.
	/// </summary>
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
}
