// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Lays out the cells of a <see cref="DetailsLayoutPage"/> row horizontally in column order.
	/// </summary>
	public sealed partial class DetailsLayoutRowPanel : Panel
	{
		private UIElement[] _orderedChildren = [];
		private int[] _orderKeys = [];

		public static readonly DependencyProperty ColumnOrderProperty =
			DependencyProperty.Register(
				nameof(ColumnOrder),
				typeof(object),
				typeof(DetailsLayoutRowPanel),
				new PropertyMetadata(null, (d, _) => ((DetailsLayoutRowPanel)d).InvalidateArrange()));

		public object? ColumnOrder
		{
			get => GetValue(ColumnOrderProperty);
			set => SetValue(ColumnOrderProperty, value);
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
