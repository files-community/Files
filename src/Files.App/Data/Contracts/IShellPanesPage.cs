﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	public interface IShellPanesPage : IDisposable, INotifyPropertyChanged
	{
		public IShellPage? ActivePane { get; set; }

		// If column view, returns the last column shell page, otherwise returns the active pane normally
		public IShellPage? ActivePaneOrColumn { get; }

		public IFilesystemHelpers? FilesystemHelpers { get; }

		public TabBarItemParameter? TabBarItemParameter { get; set; }

		public void OpenSecondaryPane(string path);

		public void CloseActivePane();

		public void AddHorizontalPane();

		public void AddVerticalPane();

		public void FocusOtherPane();

		public bool IsLeftPaneActive { get; }

		public bool IsRightPaneActive { get; }

		// Another pane is shown
		public bool IsMultiPaneActive { get; }

		// Multi pane is enabled
		public bool CanBeDualPane { get; }
	}
}
