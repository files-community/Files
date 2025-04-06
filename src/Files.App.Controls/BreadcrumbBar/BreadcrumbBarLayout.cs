// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Controls
{
	/// <summary>
	/// Handles layout of <see cref="BreadcrumbBar"/>, collapsing items into an ellipsis button when necessary.
	/// </summary>
	public partial class BreadcrumbBarLayout : NonVirtualizingLayout
	{
		// Fields

		private readonly WeakReference<BreadcrumbBar>? _ownerRef;

		private Size _availableSize;
		private BreadcrumbBarItem? _ellipsisButton = null;

		// Properties

		public bool EllipsisIsRendered { get; private set; }
		public int IndexAfterEllipsis { get; private set; }
		public int VisibleItemsCount { get; private set; }

		public BreadcrumbBarLayout(BreadcrumbBar breadcrumb)
		{
			_ownerRef = new(breadcrumb);
		}

		protected override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
		{
			var accumulatedSize = new Size(0, 0);
			_availableSize = availableSize;

			// Go through all items and measure them
			foreach (var item in context.Children)
			{
				if (item is BreadcrumbBarItem breadcrumbItem)
				{
					breadcrumbItem.Measure(availableSize);
					accumulatedSize.Width += breadcrumbItem.DesiredSize.Width;
					accumulatedSize.Height = Math.Max(accumulatedSize.Height, breadcrumbItem.DesiredSize.Height);
				}
			}

			// Get a reference to the ellipsis item
			if (context.Children.Count > 0)
				_ellipsisButton ??= context.Children[0] as BreadcrumbBarItem;

			// Sets the ellipsis item's visibility based on whether the items are overflowing
			EllipsisIsRendered = accumulatedSize.Width > availableSize.Width;

			return accumulatedSize;
		}

		protected override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
		{
			double accumulatedWidths = 0d;

			IndexAfterEllipsis = GetFirstIndexToRender(context);
			VisibleItemsCount = 0;

			// Go through all items and arrange them
			for (int index = 0; index < context.Children.Count; index++)
			{
				if (context.Children[index] is BreadcrumbBarItem breadcrumbItem)
				{
					if (index < IndexAfterEllipsis)
					{
						// Collapse
						breadcrumbItem.Arrange(new Rect(0, 0, 0, 0));
					}
					else
					{
						// Arrange normally
						breadcrumbItem.Arrange(new Rect(accumulatedWidths, 0, breadcrumbItem.DesiredSize.Width, breadcrumbItem.DesiredSize.Height));

						accumulatedWidths += breadcrumbItem.DesiredSize.Width;

						VisibleItemsCount++;
					}
				}
			}

			if (_ownerRef?.TryGetTarget(out var breadcrumbBar) ?? false)
				breadcrumbBar.OnLayoutUpdated();

			finalSize.Width = accumulatedWidths;

			return finalSize;
		}

		private int GetFirstIndexToRender(NonVirtualizingLayoutContext context)
		{
			var itemCount = context.Children.Count;
			var accumulatedWidth = 0d;

			// Go through all items from the last item
			for (int index = itemCount - 1; index >= 0; index--)
			{
				var newAccumulatedWidth = accumulatedWidth + context.Children[index].DesiredSize.Width;
				if (newAccumulatedWidth >= _availableSize.Width)
					return index + 1;

				accumulatedWidth = newAccumulatedWidth;
			}

			return 0;
		}
	}
}
