// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.UserControls;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

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
		private bool isRefreshQueued;
		private double actionColumnMinWidth;
		private static readonly Thickness ItemDividerThickness = new(0, 0, 0, 1);
		private static readonly Thickness NoBorderThickness = new(0);
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
			ViewModel.PropertyChanged += OnViewModelPropertyChanged;
			RebuildPreview();
			QueueRefreshColumnWidths();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			ViewModel.CloseRequested -= OnCloseRequested;
			ViewModel.PreviewChanged -= RebuildPreview;
			ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
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

		private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ToolbarCustomizationViewModel.SelectedToolbarContextId))
				QueueRefreshColumnWidths();
		}

		private void ToolbarItemsList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.ItemContainer is ListViewItem container)
			{
				SyncRowColumnWidths(container);
			}
			QueueRefreshColumnWidths();
		}

		// --- Column-width synchronization ---

		private void QueueRefreshColumnWidths()
		{
			if (isRefreshQueued || !IsLoaded) return;
			isRefreshQueued = true;
			DispatcherQueue.TryEnqueue(() =>
			{
				isRefreshQueued = false;
				RefreshColumnWidths();
			});
		}

		private void RefreshColumnWidths()
		{
			actionColumnMinWidth = ComputeActionColumnMinWidth();
			ToolbarItemsHeaderGrid.ColumnDefinitions[0].MinWidth = actionColumnMinWidth;
			for (int i = 0; i < ToolbarItemsList.Items.Count; i++)
				if (ToolbarItemsList.ContainerFromIndex(i) is ListViewItem container)
					SyncRowColumnWidths(container);
		}

		private double ComputeActionColumnMinWidth()
		{
			double maxWidth = 0;
			var infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			var scale = XamlRoot?.RasterizationScale ?? 1.0;
			for (int i = 0; i < ToolbarItemsList.Items.Count; i++)
			{
				if (ToolbarItemsList.ContainerFromIndex(i) is ListViewItem { ContentTemplateRoot: Grid grid }
					&& grid.Children.OfType<TextBlock>().FirstOrDefault() is { } tb)
				{
					var probe = new TextBlock
					{
						Text = tb.Text,
						FontFamily = tb.FontFamily,
						FontSize = tb.FontSize,
						FontWeight = tb.FontWeight,
						FontStyle = tb.FontStyle,
						FontStretch = tb.FontStretch,
						CharacterSpacing = tb.CharacterSpacing,
						TextWrapping = TextWrapping.NoWrap,
					};
					probe.Measure(infiniteSize);
					maxWidth = Math.Max(maxWidth, probe.DesiredSize.Width + tb.Margin.Left + tb.Margin.Right);
				}
			}

			// Round up to the nearest device pixel to avoid sub-pixel trimming
			return Math.Ceiling(maxWidth * scale) / scale;
		}

		private void SyncRowColumnWidths(ListViewItem container)
		{
			if (container.ContentTemplateRoot is not Grid { ColumnDefinitions.Count: >= 4 } grid)
				return;
			var headerCols = ToolbarItemsHeaderGrid.ColumnDefinitions;
			grid.ColumnDefinitions[0].MinWidth = actionColumnMinWidth;
			var iconWidth = headerCols[1].ActualWidth;
			if (double.IsFinite(iconWidth) && iconWidth > 0)
				grid.ColumnDefinitions[1].Width = new GridLength(iconWidth);
			var labelWidth = headerCols[2].ActualWidth;
			if (double.IsFinite(labelWidth) && labelWidth > 0)
				grid.ColumnDefinitions[2].Width = new GridLength(labelWidth);
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
			QueueRefreshColumnWidths();
		}

		private void OnCloseRequested(object? sender, EventArgs e)
		{
			skipSessionRestoreOnUnload = true;
			hostWindow?.Close();
		}

		private void TreeViewItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (sender is not FrameworkElement element)
				return;

			for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
			{
				if (current is TreeViewItem treeItem)
				{
					treeItem.IsExpanded = !treeItem.IsExpanded;
					e.Handled = true;
					break;
				}
			}
		}
	}
}
