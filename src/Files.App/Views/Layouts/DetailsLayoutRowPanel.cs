// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Lays out the cells of a <see cref="DetailsLayoutPage"/> row horizontally in column order.
	/// </summary>
	public sealed partial class DetailsLayoutRowPanel : Panel
	{
		private readonly Dictionary<string, TextBlock> _propertyCells = [];
		private UIElement[] _orderedChildren = [];
		private int[] _orderKeys = [];

		public static readonly DependencyProperty ColumnOrderProperty =
			DependencyProperty.Register(
				nameof(ColumnOrder),
				typeof(object),
				typeof(DetailsLayoutRowPanel),
				new PropertyMetadata(null, (d, _) => ((DetailsLayoutRowPanel)d).OnColumnOrderChanged()));

		public object? ColumnOrder
		{
			get => GetValue(ColumnOrderProperty);
			set => SetValue(ColumnOrderProperty, value);
		}

		public static readonly DependencyProperty PropertyCellStyleProperty =
			DependencyProperty.Register(
				nameof(PropertyCellStyle),
				typeof(Style),
				typeof(DetailsLayoutRowPanel),
				new PropertyMetadata(null));

		public Style? PropertyCellStyle
		{
			get => (Style?)GetValue(PropertyCellStyleProperty);
			set => SetValue(PropertyCellStyleProperty, value);
		}

		public DetailsLayoutRowPanel()
		{
			DataContextChanged += (_, _) => RefreshPropertyCells();
		}

		public void RefreshPropertyCells()
		{
			if (_propertyCells.Count == 0)
				return;

			var values = (DataContext as ListedItem)?.ShellPropertyValues;
			foreach (var (id, cell) in _propertyCells)
				cell.Text = values is not null && values.TryGetValue(id, out var value) ? value.Text ?? string.Empty : string.Empty;
		}

		private void OnColumnOrderChanged()
		{
			var columns = (ColumnOrder as DetailsLayoutColumnOrder)?.Columns;
			var propertyColumnIds = columns?.PropertyColumnIds ?? [];

			foreach (var id in _propertyCells.Keys.Where(id => !propertyColumnIds.Contains(id)).ToArray())
			{
				Children.Remove(_propertyCells[id]);
				_propertyCells.Remove(id);
			}

			if (columns is not null)
			{
				foreach (var id in propertyColumnIds)
				{
					if (!_propertyCells.TryGetValue(id, out var cell))
					{
						cell = new() { Padding = new(10, 0, 0, 0), Style = PropertyCellStyle };
						DetailsLayoutColumnExtensions.SetId(cell, id);
						_propertyCells[id] = cell;
						Children.Add(cell);
					}

					cell.SetBinding(WidthProperty, new Binding
					{
						Source = columns.GetColumn(id),
						Path = new PropertyPath(nameof(DetailsLayoutColumnItem.LengthIncludingGridSplitterPixels)),
						Mode = BindingMode.OneWay,
					});
				}

				RefreshPropertyCells();
			}

			InvalidateMeasure();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var childAvailableSize = new Size(double.PositiveInfinity, availableSize.Height);
			double width = 0, height = 0;

			foreach (var child in Children)
			{
				child.Measure(childAvailableSize);
				width += child.DesiredSize.Width;
				height = Math.Max(height, child.DesiredSize.Height);
			}

			return new(width, height);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var order = ColumnOrder as DetailsLayoutColumnOrder ?? DetailsLayoutColumnOrder.Default;
			var children = Children;
			var count = children.Count;

			if (_orderedChildren.Length < count)
			{
				_orderedChildren = new UIElement[count];
				_orderKeys = new int[count];
			}

			for (int i = 0; i < count; i++)
			{
				var child = children[i];
				var key = order.IndexOf(DetailsLayoutColumnExtensions.GetId(child));

				int j = i - 1;
				while (j >= 0 && _orderKeys[j] > key)
				{
					_orderKeys[j + 1] = _orderKeys[j];
					_orderedChildren[j + 1] = _orderedChildren[j];
					j--;
				}

				_orderKeys[j + 1] = key;
				_orderedChildren[j + 1] = child;
			}

			double x = 0;
			for (int i = 0; i < count; i++)
			{
				var child = _orderedChildren[i];
				var width = child.DesiredSize.Width;
				child.Arrange(new Rect(x, 0, width, Math.Max(finalSize.Height, child.DesiredSize.Height)));
				x += width;
				_orderedChildren[i] = null!;
			}

			return finalSize;
		}
	}
}
