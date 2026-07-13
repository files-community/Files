// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;
using System.ComponentModel;

namespace Files.App.Controls
{
	public partial class TableViewRow : Panel
	{
		private WeakReference<TableView>? _owner;
		private object? _dataItem;
		private INotifyPropertyChanged? _observableDataItem;

		private double _availableHeight;

		public TableViewRow()
		{
			Unloaded += TableViewRow_Unloaded;
		}

		public void SetOwner(TableView owner)
		{
			_owner = new(owner);
		}

		internal void Bind(TableView owner, object dataItem)
		{
			var isSameOwner = _owner is not null &&
				_owner.TryGetTarget(out var currentOwner) &&
				currentOwner == owner;
			var isSameDataItem = ReferenceEquals(_dataItem, dataItem);
			if (!isSameOwner || !isSameDataItem)
			{
				EndEditingCells();
				SetOwner(owner);
				SetDataItem(dataItem);
			}

			_availableHeight = 0;
			SynchronizeCells(owner);
			if (isSameOwner && isSameDataItem)
			{
				foreach (var cell in Children.OfType<TableViewCell>())
					cell.Refresh();
			}

			InvalidateArrange();
			InvalidateMeasure();
		}

		private void TableViewRow_Unloaded(object sender, RoutedEventArgs e)
		{
			Unbind();
		}

		internal void Unbind()
		{
			EndEditingCells();
			SetDataItem(null);
			_owner = null;
			_availableHeight = 0;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (_owner is null || !_owner.TryGetTarget(out var owner))
				return finalSize;

			double x = 0;
			double maxHeight = 0;

			foreach (var child in Children)
				maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);

			_availableHeight = double.IsInfinity(finalSize.Height) || finalSize.Height <= 0
				? maxHeight
				: Math.Max(maxHeight, finalSize.Height);

			foreach (var child in Children.OfType<TableViewCell>())
			{
				if (child.Column is not { } column)
					continue;

				var columnIndex = owner.GetColumnIndex(column);
				if (columnIndex < 0)
					continue;

				child.Arrange(new(
					owner.GetColumnOffset(columnIndex),
					0,
					column.ActualWidth,
					_availableHeight));
			}

			x = owner.GetTotalColumnsWidth();

			var arrangedWidth = double.IsInfinity(finalSize.Width) || finalSize.Width <= 0
				? x
				: Math.Max(x, finalSize.Width);

			return new(arrangedWidth, _availableHeight);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (_owner is null || !_owner.TryGetTarget(out var owner))
				return new(0, 0);

			owner.ResolveColumnWidths(availableSize.Width);
			SynchronizeCells(owner);

			bool desiredWidthChanged = false;
			foreach (var cell in Children.OfType<TableViewCell>())
			{
				if (cell.Column is not { ColumnWidth.IsAuto: true } column)
					continue;

				cell.Measure(new(double.PositiveInfinity, availableSize.Height));
				desiredWidthChanged |= column.ReportAutoDesiredWidth(cell.DesiredSize.Width);
			}

			if (desiredWidthChanged)
			{
				owner.InvalidateColumnWidths();
				owner.ResolveColumnWidths(availableSize.Width);
			}

			double maxHeight = 0;

			foreach (var child in Children.OfType<TableViewCell>())
			{
				if (child.Column is not { } column)
					continue;

				child.Measure(new(column.ActualWidth, availableSize.Height));

				maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
			}

			_availableHeight = maxHeight;
			var totalWidth = owner.GetTotalColumnsWidth();

			var measuredWidth = double.IsInfinity(availableSize.Width) || availableSize.Width <= 0
				? totalWidth
				: Math.Max(totalWidth, availableSize.Width);

			return new(measuredWidth, _availableHeight);
		}

		private void SynchronizeCells(TableView owner)
		{
			if (_dataItem is null)
				return;

			var (startIndex, endIndex) = owner.GetRealizedColumnRange();
			var desiredColumns = startIndex < 0
				? []
				: owner.ActiveColumns.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
			foreach (var editingCell in Children.OfType<TableViewCell>().Where(cell => cell.IsEditing))
			{
				if (editingCell.Column is { } column && owner.ActiveColumns.Contains(column) && !desiredColumns.Contains(column))
					desiredColumns.Add(column);
			}

			desiredColumns.Sort((left, right) => owner.GetColumnIndex(left).CompareTo(owner.GetColumnIndex(right)));

			for (int index = Children.Count - 1; index >= 0; index--)
			{
				if (Children[index] is TableViewCell cell &&
					cell.Column is { } column &&
					desiredColumns.Contains(column))
				{
					continue;
				}

				if (Children[index] is TableViewCell removedCell)
					removedCell.EnsureEndEdit();

				Children.RemoveAt(index);
			}

			for (int desiredIndex = 0; desiredIndex < desiredColumns.Count; desiredIndex++)
			{
				var column = desiredColumns[desiredIndex];
				int existingIndex = -1;
				for (int index = desiredIndex; index < Children.Count; index++)
				{
					if (Children[index] is TableViewCell { Column: var existingColumn } && existingColumn == column)
					{
						existingIndex = index;
						break;
					}
				}

				TableViewCell cell;
				if (existingIndex < 0)
				{
					cell = new()
					{
						VerticalAlignment = VerticalAlignment.Stretch,
						HorizontalAlignment = HorizontalAlignment.Stretch,
					};
					Children.Insert(desiredIndex, cell);
				}
				else
				{
					cell = (TableViewCell)Children[existingIndex];
					if (existingIndex != desiredIndex)
					{
						Children.RemoveAt(existingIndex);
						Children.Insert(desiredIndex, cell);
					}
				}

				if (cell.Column != column || !ReferenceEquals(cell.Data, _dataItem))
					cell.Bind(column, _dataItem);
			}
		}

		private void SetDataItem(object? dataItem)
		{
			if (_observableDataItem is not null)
				_observableDataItem.PropertyChanged -= DataItem_PropertyChanged;

			_dataItem = dataItem;
			_observableDataItem = dataItem as INotifyPropertyChanged;
			if (_observableDataItem is not null)
				_observableDataItem.PropertyChanged += DataItem_PropertyChanged;
		}

		private void DataItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (_dataItem is null)
				return;

			foreach (var cell in Children.OfType<TableViewCell>())
			{
				if (string.IsNullOrEmpty(e.PropertyName) || cell.Column?.Binding == e.PropertyName)
				{
					cell.Refresh();
					if (cell.Column is { } column &&
						_owner is not null &&
						_owner.TryGetTarget(out var owner))
					{
						owner.InvalidateAutoColumnWidth(column);
					}
				}
			}
		}

		private void EndEditingCells()
		{
			foreach (var cell in Children.OfType<TableViewCell>())
				cell.EnsureEndEdit();
		}

		internal bool CommitEdit()
		{
			foreach (var cell in Children.OfType<TableViewCell>().Where(cell => cell.IsEditing))
			{
				if (!cell.CommitEdit())
					return false;
			}

			return true;
		}

		internal void CancelEdit()
		{
			foreach (var cell in Children.OfType<TableViewCell>().Where(cell => cell.IsEditing))
				cell.CancelEdit();
		}

		internal void CancelEdit(TableViewColumn column)
		{
			foreach (var cell in Children.OfType<TableViewCell>().Where(cell => cell.IsEditing && cell.Column == column))
				cell.CancelEdit();
		}
	}
}
