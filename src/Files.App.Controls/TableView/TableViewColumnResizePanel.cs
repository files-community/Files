// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;
using Microsoft.UI.Xaml.Shapes;

namespace Files.App.Controls
{
	public sealed partial class TableViewColumnResizePanel : Panel
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			double totalWidth = 0;
			double maxHeight = 0;

			foreach (var child in Children)
			{
				if (IsAdornment(child))
				{
					child.Measure(new(double.PositiveInfinity, availableSize.Height));
					continue;
				}

				child.Measure(new(double.PositiveInfinity, availableSize.Height));
				totalWidth += child.DesiredSize.Width;
				maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
			}

			return new(totalWidth, maxHeight);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			double offset = 0;
			var boundaries = new Dictionary<TableViewColumn, double>();

			foreach (var child in Children)
			{
				if (IsAdornment(child))
					continue;

				var width = child.DesiredSize.Width;
				child.Arrange(new(offset, 0, width, finalSize.Height));
				offset += width;

				if (GetColumn(child) is { } column)
					boundaries[column] = offset;
			}

			foreach (var child in Children)
			{
				if (child is not FrameworkElement { Tag: TableViewColumn column } adornment ||
					!boundaries.TryGetValue(column, out var boundary))
				{
					continue;
				}

				var width = adornment.DesiredSize.Width;
				adornment.Arrange(new(
					Math.Max(0, boundary - width / 2),
					0,
					width,
					finalSize.Height));
			}

			return new(offset, finalSize.Height);
		}

		private static bool IsAdornment(UIElement element)
		{
			return element is ResizeVisual or Rectangle;
		}

		private static TableViewColumn? GetColumn(UIElement element)
		{
			return element as TableViewColumn ?? (element as ContentPresenter)?.Content as TableViewColumn;
		}
	}
}
