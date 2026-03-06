// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation.Collections;

namespace Files.App.Controls
{
	public partial class TableView : Control
	{
		private const string TemplatePartName_ColumnsPanel = "PART_ColumnsPanel";

		protected internal TableViewColumn? SortedColumn;

		private ReorderableItemsControl? _columnsItemsControl;
		private readonly HashSet<ResizeVisual> _trackedResizeVisuals = [];

		public TableView()
		{
			Columns = [];

			DefaultStyleKey = typeof(TableView);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UnhookColumnsPanel();
			_columnsItemsControl = GetTemplateChild(TemplatePartName_ColumnsPanel) as ReorderableItemsControl
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ColumnsPanel} in the given {nameof(TableView)}'s style.");
			HookColumnsPanel();

			Unloaded += TableView_Unloaded;

			foreach (var column in Columns)
				column.EnsureOwner(this);

			if (View is ListViewBase listViewBase)
			{
				listViewBase.ContainerContentChanging -= ListViewBase_ContainerContentChanging;
				listViewBase.Items.VectorChanged -= ListViewBase_Items_VectorChanged;
				listViewBase.Unloaded -= ListViewBase_Unloaded;

				listViewBase.ContainerContentChanging += ListViewBase_ContainerContentChanging;
				listViewBase.Items.VectorChanged += ListViewBase_Items_VectorChanged;
				listViewBase.Unloaded += ListViewBase_Unloaded;
			}
		}

		private void TableView_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= TableView_Unloaded;
			UnhookColumnsPanel();
		}

		private void Columns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Add or
				NotifyCollectionChangedAction.Replace or
				NotifyCollectionChangedAction.Reset)
			{
				foreach (var column in Columns)
					column.EnsureOwner(this);
			}

			RefreshVisibleRows();
			InvalidateLayoutOfAllRows();
		}

		private void ColumnsSource_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			SynchronizeColumnsFromSource();
		}

		private void ListViewBase_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			//Debug.WriteLine($"ListViewBase_ContainerContentChanging {args.ItemIndex}");

			var itemContainer = args.ItemContainer as Control;
			RecycleRowOf(sender, itemContainer, args.ItemIndex);

			// Recycle the index 1 item since ContainerContentChanging doesn't get called for the index 1st item somehow.
			if (args.ItemIndex is 1 && sender.ContainerFromIndex(0) is Control container)
			{
				RecycleRowOf(sender, container, 0);
			}
		}

		private void ListViewBase_Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
		{
			if (View is not ListViewBase listViewBase)
				return;

			// Only need to handle Inserted and Removed because we'll handle everything else in the ListViewBase_ContainerContentChanging event
			if (args.CollectionChange is CollectionChange.ItemInserted or CollectionChange.ItemRemoved)
			{
				Debug.WriteLine($"ListViewBase_Items_VectorChanged {args.Index}~{sender.Count}");

				int index = (int)args.Index;
				for (int i = index; i < sender.Count; i++)
				{
					var itemContainer = listViewBase.ContainerFromIndex(i) as Control;
					if (itemContainer != null)
					{
						RecycleRowOf(listViewBase, itemContainer, i);
					}
				}
			}
		}

		private void ListViewBase_Unloaded(object sender, RoutedEventArgs e)
		{
			if (sender is ListViewBase listViewBase)
			{
				listViewBase.Unloaded -= ListViewBase_Unloaded;
				listViewBase.ContainerContentChanging -= ListViewBase_ContainerContentChanging;
				listViewBase.Items.VectorChanged -= ListViewBase_Items_VectorChanged;
			}
		}

		private void RecycleRowOf(ListViewBase sender, FrameworkElement itemContainer, int itemIndex)
		{
			if (itemContainer is not ListViewItem listViewItem ||
				listViewItem.ContentTemplateRoot is not TableViewRow row ||
				sender.Items.ElementAt(itemIndex) is not ITableViewCellValueProvider cellValueProvider)
				return;

			// Ensure row content occupies the whole container height, otherwise
			// clicks in the item's empty vertical area won't hit cells.
			if (listViewItem.VerticalAlignment is not VerticalAlignment.Stretch)
				listViewItem.VerticalAlignment = VerticalAlignment.Stretch;
			if (listViewItem.VerticalContentAlignment is not VerticalAlignment.Stretch)
				listViewItem.VerticalContentAlignment = VerticalAlignment.Stretch;
			if (row.VerticalAlignment is not VerticalAlignment.Stretch)
				row.VerticalAlignment = VerticalAlignment.Stretch;

			row.Bind(this, cellValueProvider);
		}

		public void InvalidateLayoutOfAllRows()
		{
			if (View is ListViewBase listViewBase)
			{
				if (listViewBase.ItemsPanelRoot is ItemsStackPanel itemsStackPanel)
				{
					for (int index = itemsStackPanel.FirstCacheIndex;
						index <= itemsStackPanel.LastCacheIndex;
						index++)
					{
						if (listViewBase.ContainerFromIndex(index) is not ListViewItem listViewItem ||
							listViewItem.ContentTemplateRoot is not TableViewRow row)
							continue;

						//Debug.WriteLine($"RearrangeRows {index}");

						row.InvalidateArrange();
						row.InvalidateMeasure();
					}
				}
			}
		}

		private void RefreshVisibleRows()
		{
			if (View is not ListViewBase listViewBase ||
				listViewBase.ItemsPanelRoot is not ItemsStackPanel itemsStackPanel)
				return;

			for (int index = itemsStackPanel.FirstCacheIndex;
				index <= itemsStackPanel.LastCacheIndex;
				index++)
			{
				if (index < 0 || index >= listViewBase.Items.Count)
					continue;

				if (listViewBase.ContainerFromIndex(index) is Control itemContainer)
					RecycleRowOf(listViewBase, itemContainer, index);
			}
		}

		private void SynchronizeColumnsFromSource()
		{
			if (ColumnsSource is null || ReferenceEquals(ColumnsSource, Columns))
				return;

			if (ColumnsSource is not IEnumerable columnsSourceEnumerable)
				throw new InvalidOperationException($"{nameof(ColumnsSource)} must implement {nameof(IEnumerable)}.");

			Columns.Clear();

			foreach (var item in columnsSourceEnumerable)
			{
				if (item is null)
					continue;

				var template = ColumnTemplateSelector?.SelectTemplate(item, this) ?? ColumnTemplate;
				if (template?.LoadContent() is not TableViewColumn column)
				{
					throw new InvalidOperationException(
						$"{nameof(ColumnsSource)} items must be {nameof(TableViewColumn)} instances or resolve to one through {nameof(ColumnTemplate)} or {nameof(ColumnTemplateSelector)}.");
				}

				column.DataContext = item;

				Columns.Add(column);
			}
		}

		private void ColumnsPanel_LayoutUpdated(object? sender, object e)
		{
			if (_columnsItemsControl?.ItemsPanelRoot is not ResizablePanel resizablePanel)
				return;

			var aliveResizeVisuals = new HashSet<ResizeVisual>();
			foreach (var child in resizablePanel.Children)
			{
				if (child is not ResizeVisual resizeVisual)
					continue;

				aliveResizeVisuals.Add(resizeVisual);
				if (_trackedResizeVisuals.Add(resizeVisual))
				{
					resizeVisual.DragDelta += ResizeVisual_DragDelta;
					resizeVisual.DragCompleted += ResizeVisual_DragCompleted;
				}
			}

			// Unhook the events for resizers that no longer exist and synchronize the cached resizers
			foreach (var staleVisual in _trackedResizeVisuals.Where(rv => !aliveResizeVisuals.Contains(rv)).ToList())
			{
				staleVisual.DragDelta -= ResizeVisual_DragDelta;
				staleVisual.DragCompleted -= ResizeVisual_DragCompleted;
				_trackedResizeVisuals.Remove(staleVisual);
			}
		}

		private void ResizeVisual_DragDelta(object sender, DragDeltaEventArgs e)
		{
			if (sender is ResizeVisual { Target: TableViewColumn column })
			{
				column.OnColumnBeingResized();
			}
		}

		private void ResizeVisual_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (sender is ResizeVisual { Target: TableViewColumn column })
			{
				column.OnColumnResizeCompleted();
			}
		}

		private void HookColumnsPanel()
		{
			if (_columnsItemsControl is null)
				return;

			_columnsItemsControl.LayoutUpdated += ColumnsPanel_LayoutUpdated;
			_columnsItemsControl.Reordered += ColumnsPanel_Reordered;
		}

		private void UnhookColumnsPanel()
		{
			if (_columnsItemsControl is not null)
			{
				_columnsItemsControl.LayoutUpdated -= ColumnsPanel_LayoutUpdated;
				_columnsItemsControl.Reordered -= ColumnsPanel_Reordered;
			}

			foreach (var resizeVisual in _trackedResizeVisuals)
			{
				resizeVisual.DragDelta -= ResizeVisual_DragDelta;
				resizeVisual.DragCompleted -= ResizeVisual_DragCompleted;
			}

			_trackedResizeVisuals.Clear();
		}

		private void ColumnsPanel_Reordered(object? sender, ReorderedItemsEventArgs e)
		{
			RefreshVisibleRows();
			InvalidateLayoutOfAllRows();
		}
	}
}
