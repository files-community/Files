﻿using Files.App.Filesystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Files.App.Contexts
{
	public interface IContentPageContext : INotifyPropertyChanged
	{
		IShellPage? ShellPage { get; }

		ContentPageTypes PageType { get; }

		Type PageLayoutType { get; }

		ListedItem? Folder { get; }

		bool HasItem { get; }
		bool HasSelection { get; }
		bool CanRefresh { get; }
		ListedItem? SelectedItem { get; }
		IReadOnlyList<ListedItem> SelectedItems { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }
		bool CanNavigateToParent { get; }

		bool IsSearchBoxVisible { get; }
	}
}
