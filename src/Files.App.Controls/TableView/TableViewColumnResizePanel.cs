// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Controls
{
	public sealed partial class TableViewColumnResizePanel : Panel
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			double width = 0;
			double height = 0;

			foreach (var child in Children)
			{
				child.Measure(new(double.PositiveInfinity, availableSize.Height));
				height = Math.Max(height, child.DesiredSize.Height);

				if (child is ResizeVisual { Tag: TableViewColumn column })
					width += GetResolvedColumnWidth(column);
			}

			return new(width, double.IsInfinity(availableSize.Height) ? height : availableSize.Height);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			double x = 0;

			foreach (var child in Children)
			{
				if (child is not ResizeVisual { Tag: TableViewColumn column })
					continue;

				x += GetResolvedColumnWidth(column);
				var width = child.DesiredSize.Width;
				child.Arrange(new(
					Math.Max(0, x - width / 2),
					0,
					width,
					finalSize.Height));
			}

			return finalSize;
		}

		private static double GetResolvedColumnWidth(TableViewColumn column)
		{
			if (!double.IsNaN(column.Width) && column.Width > 0)
				return column.Width;

			return column.ActualWidth > 0 ? column.ActualWidth : column.MinWidth;
		}
	}
}
