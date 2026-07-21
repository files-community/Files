// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.UserControls;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Toolbar customization page. Handles drag-and-drop, preview rebuilding,
	/// column-width synchronization, and window lifecycle coordination.
	/// </summary>
	public sealed partial class ToolbarCustomizationPage : Page
	{
		private ToolbarCustomizationViewModel ViewModel => (ToolbarCustomizationViewModel)DataContext;
		private ToolbarItemDescriptor? draggedAvailableItem;
		private WindowEx? hostWindow;
		private bool skipSessionRestoreOnUnload;
		public FrameworkElement TitleBarElement => WindowTitleBar;

		public ToolbarCustomizationPage()
		{
			DataContext = Ioc.Default.GetRequiredService<ToolbarCustomizationViewModel>();
			InitializeComponent();
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			hostWindow = e.Parameter as WindowEx;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			ViewModel.BeginToolbarCustomizationSession();
			skipSessionRestoreOnUnload = false;
			ViewModel.CloseRequested += OnCloseRequested;
			ViewModel.PreviewChanged += RebuildPreview;
			RebuildPreview();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			ViewModel.CloseRequested -= OnCloseRequested;
			ViewModel.PreviewChanged -= RebuildPreview;
			if (!skipSessionRestoreOnUnload)
				ViewModel.FinishCustomizationSession(persistChanges: false);
		}

		private void RebuildPreview(object? sender = null, EventArgs? e = null)
		{
			PreviewCommandBar.PrimaryCommands.Clear();
			var style = (Style)Resources["ToolBarAppBarButtonFlyoutStyle"];
			var items = ViewModel.IsSelectedContextAlwaysVisible ? ViewModel.AlwaysVisibleToolbarItems : ViewModel.ToolbarItems;

			foreach (var item in items)
				if (Toolbar.CreatePreviewElement(item, style) is { } el)
					PreviewCommandBar.PrimaryCommands.Add(el);

			Toolbar.UpdateCommandBarSeparatorVisibility(PreviewCommandBar.PrimaryCommands);
		}

		// --- Drag-and-drop ---

		private void AvailableToolbarItemsTree_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs e)
		{
			var treeItem = e.Items.OfType<ToolbarAvailableTreeItem>().FirstOrDefault() ?? sender.SelectedItem as ToolbarAvailableTreeItem;
			if (treeItem?.ToolbarItem is not { } item)
			{
				draggedAvailableItem = null;
				e.Cancel = true;
				e.Data.RequestedOperation = DataPackageOperation.None;
				return;
			}
			draggedAvailableItem = item;
			e.Data.RequestedOperation = DataPackageOperation.Copy;
		}

		private void AvailableToolbarItemsTree_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
			=> draggedAvailableItem = null;

		private void AddedToolbarItemsList_DragOver(object sender, DragEventArgs e)
			=> e.AcceptedOperation = DataPackageOperation.Copy;

		private void AddedToolbarItemsList_Drop(object sender, DragEventArgs e)
		{
			if (draggedAvailableItem is null || sender is not ListView list)
				return;

			var insertIndex = list.Items.Count;
			for (int i = 0; i < list.Items.Count; i++)
				if (list.ContainerFromIndex(i) is ListViewItem container && e.GetPosition(container).Y < container.ActualHeight / 2)
				{
					insertIndex = i;
					break;
				}

			ViewModel.InsertAvailableToolbarItemAt(draggedAvailableItem, insertIndex);
			draggedAvailableItem = null;
		}

		private void OnCloseRequested(object? sender, EventArgs e)
		{
			skipSessionRestoreOnUnload = true;
			hostWindow?.Close();
		}

		private void AvailableToolbarItemsTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
		{
			if (args.InvokedItem is not ToolbarAvailableTreeItem { Children.Count: > 0 } item)
				return;
			if (sender.ContainerFromItem(item) is TreeViewItem container)
				container.IsExpanded = !container.IsExpanded;
		}
	}
}
