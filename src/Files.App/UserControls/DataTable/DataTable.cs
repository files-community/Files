using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Specialized;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls.DataTable
{
	internal class DataTable : Panel
	{
		// TODO: We should cache this result and update if column properties change
		internal bool IsAnyColumnAuto => Children.Any(static e => e is DataColumn { CurrentWidth.GridUnitType: GridUnitType.Auto });

		// TODO: Check with Sergio if there's a better structure here, as I don't need a Dictionary like ConditionalWeakTable
		internal HashSet<DataRow> Rows { get; private set; } = new();

		internal void ColumnResized()
		{
			InvalidateArrange();

			foreach (var row in Rows)
			{
				row.InvalidateArrange();
			}
		}

		//// TODO: Would we want this named 'Spacing' instead if we support an Orientation in the future for columns being items instead of rows?
		/// <summary>
		/// Gets or sets the amount of space to place between columns within the table.
		/// </summary>
		public double ColumnSpacing
		{
			get { return (double)GetValue(ColumnSpacingProperty); }
			set { SetValue(ColumnSpacingProperty, value); }
		}

		/// <summary>
		/// Gets the <see cref="ColumnSpacing"/> <see cref="DependencyProperty"/>.
		/// </summary>
		public static readonly DependencyProperty ColumnSpacingProperty =
			DependencyProperty.Register(nameof(ColumnSpacing), typeof(double), typeof(DataTable), new PropertyMetadata(0d));

		protected override Size MeasureOverride(Size availableSize)
		{
			double fixedWidth = 0;
			double proportionalUnits = 0;
			double autoSized = 0;

			double maxHeight = 0;

			var elements = Children.Where(static e => e.Visibility == Visibility.Visible && e is DataColumn);

			// We only need to measure elements that are visible
			foreach (DataColumn column in elements)
			{
				if (column.CurrentWidth.IsStar)
				{
					proportionalUnits += column.DesiredWidth.Value;
				}
				else if (column.CurrentWidth.IsAbsolute)
				{
					fixedWidth += column.DesiredWidth.Value;
				}
			}

			// Add in spacing between columns to our fixed size allotment
			fixedWidth += (elements.Count() - 1) * ColumnSpacing;

			// TODO: Handle infinite width?
			var proportionalAmount = (availableSize.Width - fixedWidth) / proportionalUnits;

			foreach (DataColumn column in elements)
			{
				if (column.CurrentWidth.IsStar)
				{
					column.Measure(new Size(proportionalAmount * column.CurrentWidth.Value, availableSize.Height));
				}
				else if (column.CurrentWidth.IsAbsolute)
				{
					column.Measure(new Size(column.CurrentWidth.Value, availableSize.Height));
				}
				else
				{
					// TODO: Technically this is using 'Auto' on the Header content
					// What the developer probably intends is it to be adjusted based on the contents of the rows...
					// To enable this scenario, we'll need to actually measure the contents of the rows for that column
					// in DataRow and figure out the maximum size to report back and adjust here in some sort of hand-shake
					// for the layout process... (i.e. get the data in the measure step, use it in the arrange step here,
					// then invalidate the child arranges [don't re-measure and cause loop]...)

					// For now, we'll just use the header content as a guideline to see if things work.
					column.Measure(new Size(availableSize.Width - fixedWidth - autoSized, availableSize.Height));

					// Keep track of already 'allotted' space, use either the maximum child size (if we know it) or the header content
					autoSized += Math.Max(column.DesiredSize.Width, column.MaxChildDesiredWidth);
				}

				maxHeight = Math.Max(maxHeight, column.DesiredSize.Height);
			}

			return new Size(availableSize.Width, maxHeight);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			double fixedWidth = 0;
			double proportionalUnits = 0;
			double autoSized = 0;

			var elements = Children.Where(static e => e.Visibility == Visibility.Visible && e is DataColumn);

			// We only need to measure elements that are visible
			foreach (DataColumn column in elements)
			{
				if (column.CurrentWidth.IsStar)
				{
					proportionalUnits += column.CurrentWidth.Value;
				}
				else if (column.CurrentWidth.IsAbsolute)
				{
					fixedWidth += column.CurrentWidth.Value;
				}
				else
				{
					autoSized += Math.Max(column.DesiredSize.Width, column.MaxChildDesiredWidth);
				}
			}

			// TODO: Handle infinite width?
			// TODO: This can go out of bounds or something around here when pushing a resized column to the right...
			var proportionalAmount = (finalSize.Width - fixedWidth - autoSized) / proportionalUnits;

			double width = 0;
			double x = 0;

			foreach (DataColumn column in elements)
			{
				if (column.CurrentWidth.IsStar)
				{
					width = proportionalAmount * column.CurrentWidth.Value;
					column.Arrange(new Rect(x, 0, width, finalSize.Height));
				}
				else if (column.CurrentWidth.IsAbsolute)
				{
					width = column.CurrentWidth.Value;
					column.Arrange(new Rect(x, 0, width, finalSize.Height));
				}
				else
				{
					// TODO: We use the comparison of sizes a lot, should we cache in the DataColumn itself?
					width = Math.Max(column.DesiredSize.Width, column.MaxChildDesiredWidth);
					column.Arrange(new Rect(x, 0, width, finalSize.Height));
				}

				x += width + ColumnSpacing;
			}

			return finalSize;
		}
	}
}
