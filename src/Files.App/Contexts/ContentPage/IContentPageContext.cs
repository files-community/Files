// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Contexts
{
	public interface IContentPageContext : INotifyPropertyChanged
	{
		IShellPage? ShellPage { get; }

		ContentPageTypes PageType { get; }

		Type PageLayoutType { get; }

		StandardItemViewModel? Folder { get; }

		bool HasItem { get; }
		bool HasSelection { get; }
		bool CanRefresh { get; }
		StandardItemViewModel? SelectedItem { get; }
		IReadOnlyList<StandardItemViewModel> SelectedItems { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }
		bool CanNavigateToParent { get; }

		bool IsSearchBoxVisible { get; }

		bool CanCreateItem { get; }

		bool IsMultiPaneEnabled { get; }
		bool IsMultiPaneActive { get; }

		bool ShowSearchUnindexedItemsMessage { get; }

		bool CanExecuteGitAction { get; }
	}
}
