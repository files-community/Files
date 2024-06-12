// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	public interface IContentPageContext : INotifyPropertyChanged
	{
		IShellPage? ShellPage { get; }

		ContentPageTypes PageType { get; }

		Type PageLayoutType { get; }

		StandardStorageItem? Folder { get; }

		bool HasItem { get; }
		bool HasSelection { get; }
		bool CanRefresh { get; }
		StandardStorageItem? SelectedItem { get; }
		IReadOnlyList<StandardStorageItem> SelectedItems { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }
		bool CanNavigateToParent { get; }

		bool IsSearchBoxVisible { get; }

		bool CanCreateItem { get; }

		bool IsMultiPaneEnabled { get; }
		bool IsMultiPaneActive { get; }

		bool IsGitRepository { get; }
		bool CanExecuteGitAction { get; }

		string? SolutionFilePath { get; }
	}
}
