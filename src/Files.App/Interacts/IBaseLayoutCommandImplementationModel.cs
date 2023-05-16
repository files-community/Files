// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;

namespace Files.App.Interacts
{
	public interface IBaseLayoutCommandImplementationModel : IDisposable
	{
		void ShowProperties(RoutedEventArgs e);

		Task OpenDirectoryInNewTab(RoutedEventArgs e);

		void OpenDirectoryInNewPane(RoutedEventArgs e);

		Task OpenInNewWindowItem(RoutedEventArgs e);

		void CreateNewFile(ShellNewEntry e);

		Task ItemPointerPressed(PointerRoutedEventArgs e);

		void PointerWheelChanged(PointerRoutedEventArgs e);

		Task DragOver(DragEventArgs e);

		Task Drop(DragEventArgs e);
	}
}
