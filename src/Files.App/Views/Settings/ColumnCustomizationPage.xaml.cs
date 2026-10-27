// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Lets the user choose the columns of <see cref="DetailsLayoutPage"/> by dragging them from a grouped list.
	/// </summary>
	public sealed partial class ColumnCustomizationPage : Page
	{
		private string[]? _draggedColumnIds;
		private WindowEx? _hostWindow;

		public ColumnCustomizationViewModel ViewModel { get; private set; } = null!;

		public FrameworkElement TitleBarElement => WindowTitleBar;

		public ColumnCustomizationPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (e.Parameter is ColumnCustomizationNavigationArguments { Window: var window, ViewModel: var viewModel })
			{
				_hostWindow = window;
				ViewModel = viewModel;
				Bindings.Update();
			}
		}

		private void AvailableColumnsTree_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs e)
		{
			_draggedColumnIds = [.. e.Items.OfType<ColumnTreeItem>().SelectMany(item => item.IsGroup ? item.Children.Select(child => child.Id) : new[] { item.Id })];
			if (_draggedColumnIds is not { Length: > 0 })
			{
				e.Cancel = true;
				return;
			}

			e.Data.RequestedOperation = DataPackageOperation.Copy;
		}

		private void AvailableColumnsTree_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
		{
			_draggedColumnIds = null;
		}

		private void ShownColumnsList_DragOver(object sender, DragEventArgs e)
		{
			if (_draggedColumnIds is not null)
				e.AcceptedOperation = DataPackageOperation.Copy;
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		private void ShownColumnsList_Drop(object sender, DragEventArgs e)
		{
			if (_draggedColumnIds is not { Length: > 0 })
				return;

			var insertIndex = ViewModel.ShownColumns.Count;
			for (int i = 0; i < ViewModel.ShownColumns.Count; i++)
			{
				if (ShownColumnsList.ContainerFromIndex(i) is ListViewItem container && e.GetPosition(container).Y < container.ActualHeight / 2)
				{
					insertIndex = i;
					break;
				}
			}

			ViewModel.AddColumns(_draggedColumnIds, insertIndex);
			_draggedColumnIds = null;
		}

		private void AvailableColumnsTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
		{
			if (args.InvokedItem is ColumnTreeItem { IsGroup: true } group)
				group.IsExpanded = !group.IsExpanded;
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void AvailableColumn_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.DataContext is ColumnTreeItem { IsGroup: false } column)
			{
				ViewModel.AddColumns([column.Id]);
				e.Handled = true;
			}
		}

		protected override void OnKeyDown(KeyRoutedEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Key == VirtualKey.Enter && AvailableColumnsTree.SelectedItem is ColumnTreeItem { IsGroup: false } column)
			{
				ViewModel.AddColumns([column.Id]);
				e.Handled = true;
			}
		}

		private void ShownColumnsList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			if (e.Items.OfType<ColumnListItem>().Any(column => column.IsPinned))
				e.Cancel = true;
		}

		private void ShownColumnsList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
		{
			ViewModel.KeepPinnedColumnFirst();
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void ShownColumnsList_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Delete && (e.OriginalSource as FrameworkElement)?.DataContext is ColumnListItem column)
			{
				ViewModel.RemoveColumn(column);
				e.Handled = true;
			}
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void RemoveColumn_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.DataContext is ColumnListItem column)
				ViewModel.RemoveColumn(column);
		}

		private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.RestoreDefaults();
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Save();
			_hostWindow?.Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			_hostWindow?.Close();
		}
	}
}
