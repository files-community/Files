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
			EndEditingCells();
			SetOwner(owner);
			SetDataItem(dataItem);
			_availableHeight = 0;

			var schemaMatches = Children.Count == owner.ActiveColumns.Count;
			if (schemaMatches)
			{
				for (int index = 0; index < Children.Count; index++)
				{
					if (Children[index] is not TableViewCell cell || cell.Column != owner.ActiveColumns[index])
					{
						schemaMatches = false;
						break;
					}
				}
			}

			if (!schemaMatches)
			{
				Children.Clear();
				foreach (var column in owner.ActiveColumns)
				{
					Children.Add(new TableViewCell
					{
						VerticalAlignment = VerticalAlignment.Stretch,
						HorizontalAlignment = HorizontalAlignment.Stretch,
					});
				}
			}

			for (int index = 0; index < owner.ActiveColumns.Count; index++)
			{
				((TableViewCell)Children[index]).Bind(owner.ActiveColumns[index], dataItem);
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

			for (int index = 0; index < Children.Count; index++)
			{
				var column = owner.ActiveColumns[index];
				var child = Children[index];

				child.Arrange(new(
					x,
					0,
					column.ActualWidth,
					_availableHeight));

				x += column.ActualWidth;
			}

			var arrangedWidth = double.IsInfinity(finalSize.Width) || finalSize.Width <= 0
				? x
				: Math.Max(x, finalSize.Width);

			return new(arrangedWidth, _availableHeight);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (Children.Count is 0 || _owner is null || !_owner.TryGetTarget(out var owner))
				return new(0, 0);

			owner.ResolveColumnWidths(availableSize.Width);

			double maxHeight = 0;
			double totalWidth = 0;

			for (int index = 0; index < Children.Count; index++)
			{
				var child = Children[index];
				var column = owner.ActiveColumns[index];

				child.Measure(new(column.ActualWidth, availableSize.Height));

				maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
				totalWidth += column.ActualWidth;
			}

			_availableHeight = maxHeight;

			var measuredWidth = double.IsInfinity(availableSize.Width) || availableSize.Width <= 0
				? totalWidth
				: Math.Max(totalWidth, availableSize.Width);

			return new(measuredWidth, _availableHeight);
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
					cell.Refresh();
			}
		}

		private void EndEditingCells()
		{
			foreach (var cell in Children.OfType<TableViewCell>())
				cell.EnsureEndEdit();
		}

	}
}
