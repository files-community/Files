// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Specialized;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation.Collections;

namespace Files.App.Controls
{
	public partial class TableView : Control
	{
		private const string TemplatePartName_ColumnsPanel = "PART_ColumnsPanel";
		private const string TemplatePartName_ColumnsScrollViewer = "PART_ColumnsScrollViewer";

		protected internal TableViewColumn? SortedColumn;

		private ReorderableItemsControl? _columnsItemsControl;
		private ScrollViewer? _columnsScrollViewer;
		private TableViewColumnResizePanel? _columnResizePanel;
		private ScrollViewer? _viewScrollViewer;
		private ListViewBase? _listView;
		private readonly HashSet<TableViewColumn> _ownedColumns = [];
		private readonly Dictionary<object, TableViewColumn> _columnsBySourceItem = new(ReferenceEqualityComparer.Instance);
		private bool _isSynchronizingColumns;
		private bool _isSynchronizingScroll;
		private bool _isUpdatingDeclaredColumnsOrder;
		private bool _isUpdatingColumnsSourceOrder;
		private TableViewColumn? _resizingColumn;
		private GridLength _resizingColumnOriginalWidth;
		private double[] _columnOffsets = [];
		private readonly Dictionary<TableViewColumn, int> _columnIndexes = [];
		private double _totalColumnsWidth;
		private (int StartIndex, int EndIndex)? _realizedColumnRange;
		private double _lastColumnWidthConstraint = double.NaN;
		private bool _columnWidthsDirty = true;

		internal ObservableCollection<TableViewColumn> ActiveColumns { get; } = [];

		public TableView()
		{
			Columns = [];
			ActiveColumns.CollectionChanged += ActiveColumns_CollectionChanged;
			SizeChanged += TableView_SizeChanged;

			DefaultStyleKey = typeof(TableView);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TableViewAutomationPeer(this);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UnhookColumnsPanel();
			UnhookView();
			_columnsItemsControl = GetTemplateChild(TemplatePartName_ColumnsPanel) as ReorderableItemsControl
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ColumnsPanel} in the given {nameof(TableView)}'s style.");
			_columnsScrollViewer = GetTemplateChild(TemplatePartName_ColumnsScrollViewer) as ScrollViewer
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ColumnsScrollViewer} in the given {nameof(TableView)}'s style.");
			_columnsItemsControl.ItemsSource = ActiveColumns;
			UpdateColumnsPanelInteractionState();
			UpdateResizeVisualInteractionState();
			HookColumnsPanel();
			HookView(View);
			SynchronizeActiveColumns();

			Loaded -= TableView_Loaded;
			Loaded += TableView_Loaded;
			Unloaded -= TableView_Unloaded;
			Unloaded += TableView_Unloaded;
		}

		private void TableView_Loaded(object sender, RoutedEventArgs e)
		{
			EnsureColumnResizePanel();
			HookColumnsPanel();
			HookView(View);
			ResolveColumnWidths();
			RefreshVisibleRows();
		}

		private void TableView_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (!e.NewSize.Width.Equals(e.PreviousSize.Width))
				ResolveColumnWidths(e.NewSize.Width);
		}

		private void TableView_Unloaded(object sender, RoutedEventArgs e)
		{
			UnhookColumnsPanel();
			UnhookView();
		}

		private void Columns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (ColumnsSource is null && !_isUpdatingDeclaredColumnsOrder)
				SynchronizeActiveColumns();
		}

		private void ColumnsSource_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (!_isUpdatingColumnsSourceOrder)
				SynchronizeActiveColumns();
		}

		private void ActiveColumns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (_isSynchronizingColumns)
				return;

			ReconcileColumnOwners();
			NotifyPropertyChanged(
				this,
				TableViewNotificationTarget.ColumnLayout |
				TableViewNotificationTarget.VisibleRows |
				TableViewNotificationTarget.RowLayout);
		}

		private void ListViewBase_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			var itemContainer = args.ItemContainer as Control;
			if (itemContainer is not ListViewItem listViewItem ||
				listViewItem.ContentTemplateRoot is not TableViewRow row)
				return;

			if (args.InRecycleQueue)
			{
				row.Unbind();
				return;
			}

			BindRow(listViewItem, row, args.Item);
		}

		private void ListViewBase_Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
		{
			if (View is not { } listViewBase)
				return;

			if (args.CollectionChange is CollectionChange.ItemRemoved or CollectionChange.Reset)
				ResetAutoColumnWidths();

			listViewBase.DispatcherQueue.TryEnqueue(RefreshVisibleRows);
		}

		private void RecycleRowOf(ListViewBase sender, FrameworkElement itemContainer, int itemIndex)
		{
			if (itemContainer is not ListViewItem listViewItem ||
				listViewItem.ContentTemplateRoot is not TableViewRow row ||
				itemIndex < 0 ||
				itemIndex >= sender.Items.Count)
				return;

			BindRow(listViewItem, row, sender.Items[itemIndex]);
		}

		private void BindRow(ListViewItem listViewItem, TableViewRow row, object? item)
		{
			if (item is null)
			{
				row.Unbind();
				return;
			}

			// Ensure row content occupies the whole container height, otherwise
			// clicks in the item's empty vertical area won't hit cells.
			if (listViewItem.VerticalAlignment is not VerticalAlignment.Stretch)
				listViewItem.VerticalAlignment = VerticalAlignment.Stretch;
			if (listViewItem.VerticalContentAlignment is not VerticalAlignment.Stretch)
				listViewItem.VerticalContentAlignment = VerticalAlignment.Stretch;
			if (listViewItem.HorizontalAlignment is not HorizontalAlignment.Stretch)
				listViewItem.HorizontalAlignment = HorizontalAlignment.Stretch;
			if (listViewItem.HorizontalContentAlignment is not HorizontalAlignment.Stretch)
				listViewItem.HorizontalContentAlignment = HorizontalAlignment.Stretch;
			if (row.VerticalAlignment is not VerticalAlignment.Stretch)
				row.VerticalAlignment = VerticalAlignment.Stretch;
			if (row.HorizontalAlignment is not HorizontalAlignment.Stretch)
				row.HorizontalAlignment = HorizontalAlignment.Stretch;

			row.Bind(this, item);
		}

		public void InvalidateLayoutOfAllRows()
		{
			if (View is { } listViewBase)
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

						row.InvalidateMeasure();
					}
				}
			}
		}

		private void RefreshVisibleRows()
		{
			if (View is not { } listViewBase ||
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

		private void SynchronizeActiveColumns()
		{
			var desiredColumns = ColumnsSource is null
				? [.. Columns]
				: CreateColumnsFromSource();

			if (desiredColumns.Count != desiredColumns.Distinct().Count())
				throw new InvalidOperationException($"A {nameof(TableViewColumn)} cannot appear more than once in a {nameof(TableView)}.");

			foreach (var column in desiredColumns)
				column.VerifyCanAttachOwner(this);

			if (!CommitEdit())
				CancelEdit();

			_isSynchronizingColumns = true;
			try
			{
				for (int index = ActiveColumns.Count - 1; index >= 0; index--)
				{
					if (!desiredColumns.Contains(ActiveColumns[index]))
						ActiveColumns.RemoveAt(index);
				}

				for (int targetIndex = 0; targetIndex < desiredColumns.Count; targetIndex++)
				{
					var column = desiredColumns[targetIndex];
					var currentIndex = ActiveColumns.IndexOf(column);
					if (currentIndex < 0)
						ActiveColumns.Insert(targetIndex, column);
					else if (currentIndex != targetIndex)
						ActiveColumns.Move(currentIndex, targetIndex);
				}
			}
			finally
			{
				_isSynchronizingColumns = false;
			}

			ReconcileColumnOwners();
			NotifyPropertyChanged(
				this,
				TableViewNotificationTarget.ColumnLayout |
				TableViewNotificationTarget.VisibleRows |
				TableViewNotificationTarget.RowLayout);
		}

		private List<TableViewColumn> CreateColumnsFromSource()
		{
			if (ColumnsSource is not IEnumerable source)
				throw new InvalidOperationException($"{nameof(ColumnsSource)} must implement {nameof(IEnumerable)}.");

			var desiredColumns = new List<TableViewColumn>();
			var aliveItems = new HashSet<object>(ReferenceEqualityComparer.Instance);
			foreach (var item in source)
			{
				if (item is null)
					continue;

				aliveItems.Add(item);
				if (item is TableViewColumn directColumn)
				{
					desiredColumns.Add(directColumn);
					continue;
				}

				if (!_columnsBySourceItem.TryGetValue(item, out var column))
				{
					var template = ColumnTemplateSelector?.SelectTemplate(item, this) ?? ColumnTemplate;
					if (template?.LoadContent() is not TableViewColumn generatedColumn)
					{
						throw new InvalidOperationException(
							$"{nameof(ColumnsSource)} items must be {nameof(TableViewColumn)} instances or resolve to one through {nameof(ColumnTemplate)} or {nameof(ColumnTemplateSelector)}.");
					}

					generatedColumn.DataContext = item;
					column = generatedColumn;
					_columnsBySourceItem[item] = column;
				}

				desiredColumns.Add(column);
			}

			foreach (var staleItem in _columnsBySourceItem.Keys.Where(item => !aliveItems.Contains(item)).ToList())
				_columnsBySourceItem.Remove(staleItem);

			return desiredColumns;
		}

		private void ReconcileColumnOwners()
		{
			foreach (var column in _ownedColumns.Where(column => !ActiveColumns.Contains(column)).ToList())
			{
				column.DetachOwner(this);
				_ownedColumns.Remove(column);
			}

			foreach (var column in ActiveColumns)
			{
				column.AttachOwner(this);
				_ownedColumns.Add(column);
			}
		}

		private void HookColumnsPanel()
		{
			if (_columnsItemsControl is null)
				return;

			_columnsItemsControl.Reordered -= ColumnsPanel_Reordered;
			_columnsItemsControl.SizeChanged -= ColumnsPanel_SizeChanged;
			_columnsItemsControl.Reordered += ColumnsPanel_Reordered;
			_columnsItemsControl.SizeChanged += ColumnsPanel_SizeChanged;

			if (_columnsScrollViewer is not null)
			{
				_columnsScrollViewer.ViewChanged -= ColumnsScrollViewer_ViewChanged;
				_columnsScrollViewer.ViewChanged += ColumnsScrollViewer_ViewChanged;
			}
		}

		private void UnhookColumnsPanel()
		{
			if (_columnsItemsControl is not null)
			{
				_columnsItemsControl.Reordered -= ColumnsPanel_Reordered;
				_columnsItemsControl.SizeChanged -= ColumnsPanel_SizeChanged;
			}

			if (_columnsScrollViewer is not null)
				_columnsScrollViewer.ViewChanged -= ColumnsScrollViewer_ViewChanged;

			UnhookColumnResizePanel();
			_columnResizePanel = null;
		}

		private void ColumnsPanel_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			EnsureColumnResizePanel();
			InvalidateColumnResizePanel();
		}

		private void ColumnsPanel_Reordered(object? sender, ReorderedItemsEventArgs e)
		{
			if (!CanUserReorderColumns)
				return;

			PersistColumnOrder(e.NewIndexToOldIndexMap);
			ColumnReordered?.Invoke(this, e);
			NotifyPropertyChanged(
				this,
				TableViewNotificationTarget.ColumnLayout |
				TableViewNotificationTarget.VisibleRows |
				TableViewNotificationTarget.RowLayout);
		}

		internal void ResolveColumnWidths(double availableWidth = double.NaN)
		{
			if (ActiveColumns.Count is 0)
			{
				_columnOffsets = [];
				_columnIndexes.Clear();
				_totalColumnsWidth = 0;
				_realizedColumnRange = null;
				return;
			}

			if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth <= 0)
			{
				availableWidth = _viewScrollViewer?.ViewportWidth ?? ActualWidth;
			}

			if (!_columnWidthsDirty && availableWidth.Equals(_lastColumnWidthConstraint))
				return;

			double occupiedWidth = 0;
			var starColumns = new List<TableViewColumn>();
			foreach (var column in ActiveColumns)
			{
				if (column.ColumnWidth.IsStar)
				{
					starColumns.Add(column);
					continue;
				}

				var width = ResolveNonStarColumnWidth(column);
				column.ApplyResolvedWidth(width);
				occupiedWidth += width;
			}

			ResolveStarColumnWidths(starColumns, Math.Max(0, availableWidth - occupiedWidth));
			UpdateColumnLayoutCache();
			_lastColumnWidthConstraint = availableWidth;
			_columnWidthsDirty = false;

			InvalidateColumnResizePanel();
		}

		public bool CommitEdit()
		{
			foreach (var row in GetRealizedRows())
			{
				if (!row.CommitEdit())
					return false;
			}

			return true;
		}

		public void CancelEdit()
		{
			foreach (var row in GetRealizedRows())
				row.CancelEdit();
		}

		internal void CancelEdit(TableViewColumn column)
		{
			foreach (var row in GetRealizedRows())
				row.CancelEdit(column);
		}

		private IEnumerable<TableViewRow> GetRealizedRows()
		{
			if (View is not { } listViewBase || listViewBase.ItemsPanelRoot is not ItemsStackPanel itemsStackPanel)
				yield break;

			for (int index = itemsStackPanel.FirstCacheIndex; index <= itemsStackPanel.LastCacheIndex; index++)
			{
				if (index >= 0 && index < listViewBase.Items.Count &&
					listViewBase.ContainerFromIndex(index) is ListViewItem { ContentTemplateRoot: TableViewRow row })
				{
					yield return row;
				}
			}
		}

		internal (int StartIndex, int EndIndex) GetRealizedColumnRange()
		{
			if (_realizedColumnRange is { } realizedColumnRange)
				return realizedColumnRange;

			if (ActiveColumns.Count is 0)
				return (-1, -1);

			if (_viewScrollViewer is not { ViewportWidth: > 0 } scrollViewer)
			{
				_realizedColumnRange = (0, ActiveColumns.Count - 1);
				return _realizedColumnRange.Value;
			}

			var cacheLength = scrollViewer.ViewportWidth * 0.25;
			var realizationStart = Math.Max(0, scrollViewer.HorizontalOffset - cacheLength);
			var realizationEnd = scrollViewer.HorizontalOffset + scrollViewer.ViewportWidth + cacheLength;
			int startIndex = -1;
			int endIndex = -1;
			for (int index = 0; index < ActiveColumns.Count; index++)
			{
				var columnStart = GetColumnOffset(index);
				var columnEnd = columnStart + ActiveColumns[index].ActualWidth;
				if (columnEnd >= realizationStart && columnStart <= realizationEnd)
				{
					startIndex = startIndex < 0 ? index : startIndex;
					endIndex = index;
				}

				if (columnStart > realizationEnd)
					break;
			}

			_realizedColumnRange = startIndex < 0 ? (0, 0) : (startIndex, endIndex);
			return _realizedColumnRange.Value;
		}

		internal double GetColumnOffset(int columnIndex)
		{
			return columnIndex >= 0 && columnIndex < _columnOffsets.Length
				? _columnOffsets[columnIndex]
				: 0;
		}

		internal int GetColumnIndex(TableViewColumn column)
		{
			return _columnIndexes.TryGetValue(column, out var index) ? index : -1;
		}

		internal double GetTotalColumnsWidth()
		{
			return _totalColumnsWidth;
		}

		private void UpdateColumnLayoutCache()
		{
			if (_columnOffsets.Length != ActiveColumns.Count)
				_columnOffsets = new double[ActiveColumns.Count];

			_columnIndexes.Clear();
			double offset = 0;
			for (int index = 0; index < ActiveColumns.Count; index++)
			{
				_columnOffsets[index] = offset;
				_columnIndexes[ActiveColumns[index]] = index;
				offset += ActiveColumns[index].ActualWidth;
			}

			_totalColumnsWidth = offset;
			_realizedColumnRange = null;
		}

		internal void InvalidateColumnWidths()
		{
			_columnWidthsDirty = true;
		}

		internal void InvalidateAutoColumnWidth(TableViewColumn column)
		{
			if (!column.ColumnWidth.IsAuto)
				return;

			column.ResetAutoDesiredWidth();
			_columnWidthsDirty = true;
			InvalidateLayoutOfAllRows();
			_columnResizePanel?.InvalidateMeasure();
		}

		private void ResetAutoColumnWidths()
		{
			foreach (var column in ActiveColumns)
				column.ResetAutoDesiredWidth();

			_columnWidthsDirty = true;
			_columnResizePanel?.InvalidateMeasure();
			InvalidateLayoutOfAllRows();
		}

		private static void ResolveStarColumnWidths(IReadOnlyList<TableViewColumn> columns, double availableWidth)
		{
			if (columns.Count is 0)
				return;

			var unresolved = columns.ToList();
			double remainingWidth = availableWidth;
			while (unresolved.Count > 0)
			{
				double totalWeight = unresolved.Sum(column => Math.Max(double.Epsilon, column.ColumnWidth.Value));
				bool constrainedColumnFound = false;

				for (int index = unresolved.Count - 1; index >= 0; index--)
				{
					var column = unresolved[index];
					var proposedWidth = totalWeight <= 0
						? 0
						: remainingWidth * column.ColumnWidth.Value / totalWeight;
					var constrainedWidth = Math.Clamp(proposedWidth, column.MinWidth, column.MaxWidth);
					if (!proposedWidth.Equals(constrainedWidth))
					{
						column.ApplyResolvedWidth(constrainedWidth);
						remainingWidth = Math.Max(0, remainingWidth - constrainedWidth);
						unresolved.RemoveAt(index);
						constrainedColumnFound = true;
					}
				}

				if (constrainedColumnFound)
					continue;

				totalWeight = unresolved.Sum(column => Math.Max(double.Epsilon, column.ColumnWidth.Value));
				foreach (var column in unresolved)
				{
					var width = totalWeight <= 0
						? column.MinWidth
						: remainingWidth * column.ColumnWidth.Value / totalWeight;
					column.ApplyResolvedWidth(Math.Clamp(width, column.MinWidth, column.MaxWidth));
				}

				break;
			}
		}

		private void InvalidateColumnResizePanel()
		{
			EnsureColumnResizePanel();
			if (_columnResizePanel is null)
				return;

			_columnResizePanel.CanResizeColumns = CanUserResizeColumns;
			_columnResizePanel.InvalidateMeasure();
		}

		private void EnsureColumnResizePanel()
		{
			var panel = _columnsItemsControl?.ItemsPanelRoot as TableViewColumnResizePanel;
			if (ReferenceEquals(panel, _columnResizePanel))
				return;

			UnhookColumnResizePanel();
			_columnResizePanel = panel;
			if (_columnResizePanel is not null)
			{
				_columnResizePanel.ColumnResizeStarted += ColumnResizeVisual_DragStarted;
				_columnResizePanel.ColumnResizeDelta += ColumnResizeVisual_DragDelta;
				_columnResizePanel.ColumnResizeCompleted += ColumnResizeVisual_DragCompleted;
				_columnResizePanel.CanResizeColumns = CanUserResizeColumns;
			}
		}

		private void UnhookColumnResizePanel()
		{
			if (_columnResizePanel is null)
				return;

			_columnResizePanel.ColumnResizeStarted -= ColumnResizeVisual_DragStarted;
			_columnResizePanel.ColumnResizeDelta -= ColumnResizeVisual_DragDelta;
			_columnResizePanel.ColumnResizeCompleted -= ColumnResizeVisual_DragCompleted;
		}

		private static double GetResolvedColumnWidth(TableViewColumn column)
		{
			if (!double.IsNaN(column.Width) && column.Width > 0)
				return column.Width;

			return column.ActualWidth > 0 ? column.ActualWidth : column.MinWidth;
		}

		private void ColumnResizeVisual_DragStarted(object sender, DragStartedEventArgs e)
		{
			if (!CanUserResizeColumns || sender is not ResizeVisual { Tag: TableViewColumn { CanUserResize: true } column })
				return;
			if (!CommitEdit())
				return;

			_resizingColumn = column;
			_resizingColumnOriginalWidth = column.ColumnWidth;
			IsColumnResizing = true;
		}

		private void ColumnResizeVisual_DragDelta(object sender, DragDeltaEventArgs e)
		{
			if (!CanUserResizeColumns || sender is not ResizeVisual { Tag: TableViewColumn { CanUserResize: true } column })
				return;
			if (_resizingColumn != column)
				return;

			var delta = FlowDirection is FlowDirection.RightToLeft ? -e.HorizontalChange : e.HorizontalChange;
			var currentWidth = GetResolvedColumnWidth(column);
			var newWidth = Math.Clamp(currentWidth + delta, column.MinWidth, column.MaxWidth);
			column.ColumnWidth = new GridLength(newWidth, GridUnitType.Pixel);
			ResolveColumnWidths();
			InvalidateLayoutOfAllRows();
		}

		private void ColumnResizeVisual_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (_resizingColumn is not null && e.Canceled)
				_resizingColumn.ColumnWidth = _resizingColumnOriginalWidth;

			_resizingColumn = null;
			IsColumnResizing = false;
			ResolveColumnWidths();
			InvalidateLayoutOfAllRows();
		}

		private static double ResolveNonStarColumnWidth(TableViewColumn column)
		{
			var width = column.ColumnWidth.IsAuto
				? column.AutoDesiredWidth
				: column.ColumnWidth.Value;

			if (width <= 0 || double.IsNaN(width))
				width = column.MinWidth;

			return Math.Clamp(width, column.MinWidth, column.MaxWidth);
		}

		private void PersistColumnOrder(IReadOnlyList<int> reorderedIndexMap)
		{
			if (ColumnsSource is null)
			{
				var reorderedColumns = reorderedIndexMap.Select(index => Columns[index]).ToArray();
				_isUpdatingDeclaredColumnsOrder = true;
				try
				{
					for (int targetIndex = 0; targetIndex < reorderedColumns.Length; targetIndex++)
					{
						var currentIndex = Columns.IndexOf(reorderedColumns[targetIndex]);
						if (currentIndex != targetIndex)
							Columns.Move(currentIndex, targetIndex);
					}
				}
				finally
				{
					_isUpdatingDeclaredColumnsOrder = false;
				}

				return;
			}

			if (ColumnsSource is not IList source ||
				source.IsReadOnly ||
				source.IsFixedSize ||
				source.Count != reorderedIndexMap.Count)
				return;

			var reorderedItems = new object?[source.Count];
			for (int index = 0; index < reorderedIndexMap.Count; index++)
				reorderedItems[index] = source[reorderedIndexMap[index]];

			_isUpdatingColumnsSourceOrder = true;
			try
			{
				for (int targetIndex = 0; targetIndex < reorderedItems.Length; targetIndex++)
				{
					var currentIndex = IndexOfReference(source, reorderedItems[targetIndex]);
					if (currentIndex < 0 || currentIndex == targetIndex)
						continue;

					var item = source[currentIndex];
					source.RemoveAt(currentIndex);
					source.Insert(targetIndex, item);
				}
			}
			finally
			{
				_isUpdatingColumnsSourceOrder = false;
			}
		}

		private static int IndexOfReference(IList source, object? item)
		{
			for (int index = 0; index < source.Count; index++)
			{
				if (ReferenceEquals(source[index], item))
					return index;
			}

			return -1;
		}

		private void HookView(ListViewBase? listView)
		{
			if (ReferenceEquals(_listView, listView))
			{
				TryHookViewScrollViewer();
				return;
			}

			UnhookView();
			if (listView is null)
				return;

			_listView = listView;
			ScrollViewer.SetHorizontalScrollMode(listView, ScrollMode.Enabled);
			ScrollViewer.SetHorizontalScrollBarVisibility(listView, ScrollBarVisibility.Auto);
			listView.ContainerContentChanging += ListViewBase_ContainerContentChanging;
			listView.Items.VectorChanged += ListViewBase_Items_VectorChanged;
			listView.Loaded += ListViewBase_Loaded;
			TryHookViewScrollViewer();
		}

		private void UnhookView()
		{
			if (_listView is not null)
			{
				_listView.ContainerContentChanging -= ListViewBase_ContainerContentChanging;
				_listView.Items.VectorChanged -= ListViewBase_Items_VectorChanged;
				_listView.Loaded -= ListViewBase_Loaded;
			}

			if (_viewScrollViewer is not null)
				_viewScrollViewer.ViewChanged -= ViewScrollViewer_ViewChanged;

			_viewScrollViewer = null;
			_realizedColumnRange = null;
			_listView = null;
		}

		private void ListViewBase_Loaded(object sender, RoutedEventArgs e)
		{
			TryHookViewScrollViewer();
			RefreshVisibleRows();
		}

		private void TryHookViewScrollViewer()
		{
			if (_listView is null)
				return;

			var scrollViewer = _listView.FindDescendant<ScrollViewer>();
			if (ReferenceEquals(scrollViewer, _viewScrollViewer))
				return;

			if (_viewScrollViewer is not null)
				_viewScrollViewer.ViewChanged -= ViewScrollViewer_ViewChanged;

			_viewScrollViewer = scrollViewer;
			_realizedColumnRange = null;
			if (_viewScrollViewer is not null)
			{
				_viewScrollViewer.ViewChanged += ViewScrollViewer_ViewChanged;
				SynchronizeHeaderScrollOffset(_viewScrollViewer.HorizontalOffset);
				ResolveColumnWidths(_viewScrollViewer.ViewportWidth);
			}
		}

		private void ViewScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
		{
			if (sender is not ScrollViewer scrollViewer)
				return;

			_realizedColumnRange = null;
			if (!_isSynchronizingScroll)
				SynchronizeHeaderScrollOffset(scrollViewer.HorizontalOffset);

			InvalidateLayoutOfAllRows();
		}

		private void ColumnsScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
		{
			if (_isSynchronizingScroll || _viewScrollViewer is null || sender is not ScrollViewer scrollViewer)
				return;

			_isSynchronizingScroll = true;
			try
			{
				_viewScrollViewer.ChangeView(scrollViewer.HorizontalOffset, null, null, true);
			}
			finally
			{
				_isSynchronizingScroll = false;
			}
		}

		private void SynchronizeHeaderScrollOffset(double horizontalOffset)
		{
			if (_columnsScrollViewer is null)
				return;

			_isSynchronizingScroll = true;
			try
			{
				_columnsScrollViewer.ChangeView(horizontalOffset, null, null, true);
			}
			finally
			{
				_isSynchronizingScroll = false;
			}
		}

		internal void RequestSort(TableViewColumn column)
		{
			if (!CanUserSortColumns || !column.CanUserSort)
				return;
			if (!CommitEdit())
				return;

			var requestedDirection = column.SortDirection is null or ListSortDirection.Descending
				? ListSortDirection.Ascending
				: ListSortDirection.Descending;
			var args = new TableViewColumnSortingEventArgs(column, requestedDirection);
			Sorting?.Invoke(this, args);
			if (args.Cancel)
				return;

			if (SortedColumn is not null && SortedColumn != column)
				SortedColumn.SortDirection = null;

			SortedColumn = column;
			column.SortDirection = args.SortDirection;
		}

		internal void RegisterInitialSortColumn(TableViewColumn column)
		{
			if (column.SortDirection is null)
				return;

			if (SortedColumn is not null && SortedColumn != column)
				SortedColumn.SortDirection = null;

			SortedColumn = column;
		}

		internal void UnregisterSortColumn(TableViewColumn column)
		{
			if (SortedColumn == column)
				SortedColumn = null;
		}

		private void UpdateColumnsPanelInteractionState()
		{
			if (_columnsItemsControl is not null)
			{
				_columnsItemsControl.IsReorderEnabled = CanUserReorderColumns;
				_columnsItemsControl.ReorderItemFilter = item =>
					item is not TableViewColumn column || column.CanUserReorder && CommitEdit();
			}
		}

		private void UpdateResizeVisualInteractionState()
		{
			EnsureColumnResizePanel();
			if (_columnResizePanel is not null)
			{
				_columnResizePanel.CanResizeColumns = CanUserResizeColumns;
				_columnResizePanel.UpdateResizeVisualInteractionState();
			}

			if ((!CanUserResizeColumns || _resizingColumn is { CanUserResize: false }) && IsColumnResizing)
				IsColumnResizing = false;
		}

		internal void NotifyPropertyChanged(DependencyObject source, TableViewNotificationTarget target)
		{
			if (target.HasFlag(TableViewNotificationTarget.ColumnLayout))
			{
				InvalidateColumnWidths();
				ResolveColumnWidths();
			}

			if (target.HasFlag(TableViewNotificationTarget.ResizeVisuals))
				UpdateColumnInteractionState();

			if (target.HasFlag(TableViewNotificationTarget.ColumnHeaders))
				_columnResizePanel?.InvalidateMeasure();

			if (target.HasFlag(TableViewNotificationTarget.VisibleRows))
				RefreshVisibleRows();

			if (target.HasFlag(TableViewNotificationTarget.RowLayout))
				InvalidateLayoutOfAllRows();
		}

		internal void UpdateColumnInteractionState()
		{
			UpdateColumnsPanelInteractionState();
			UpdateResizeVisualInteractionState();
		}
	}
}
