// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Controls
{
	public partial class SnapPanel : Panel
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			foreach (UIElement child in Children)
				child.Measure(new Size(double.PositiveInfinity, availableSize.Height));

			double width = Children.Sum(c => c.DesiredSize.Width);
			double height = Children.Max(c => c.DesiredSize.Height);

			return new Size(width, height);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			double x = 0;
			foreach (UIElement child in Children)
			{
				var width = child.DesiredSize.Width;
				child.Arrange(new Rect(x, 0, width, finalSize.Height));
				x += width;
			}
			return finalSize;
		}
	}
}
