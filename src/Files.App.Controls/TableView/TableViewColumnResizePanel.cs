// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;
using Microsoft.UI.Xaml.Shapes;

namespace Files.App.Controls
{
	public sealed partial class TableViewColumnResizePanel : Panel
	{
		private const int DividerZIndex = 1;
		private const int ResizeVisualZIndex = 2;

		private readonly Dictionary<TableViewColumn, ResizeVisual> _resizeVisuals = [];
		private readonly Dictionary<TableViewColumn, Rectangle> _dividers = [];
		private bool _canResizeColumns = true;

		internal event DragStartedEventHandler? ColumnResizeStarted;
		internal event DragDeltaEventHandler? ColumnResizeDelta;
		internal event DragCompletedEventHandler? ColumnResizeCompleted;

		internal bool CanResizeColumns
		{
			get => _canResizeColumns;
			set
			{
				if (_canResizeColumns == value)
					return;

				_canResizeColumns = value;
				UpdateResizeVisualInteractionState();
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var contentChildren = Children.Where(child => !IsAdornment(child)).ToList();
			EnsureAdornments(contentChildren);
			TableView? owner = null;
			bool desiredWidthChanged = false;
			foreach (var child in contentChildren)
			{
				if (GetColumn(child) is not { } column)
					continue;

				owner ??= column.GetOwner();
				desiredWidthChanged |= column.MeasureHeaderDesiredWidth(availableSize.Height);
			}

			if (desiredWidthChanged)
			{
				owner?.InvalidateColumnWidths();
				owner?.ResolveColumnWidths(availableSize.Width);
			}

			double totalWidth = 0;
			double maxHeight = 0;

			foreach (var child in contentChildren)
			{
				child.Measure(new(double.PositiveInfinity, availableSize.Height));
				totalWidth += child.DesiredSize.Width;
				maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
			}

			foreach (var adornment in Children.Where(IsAdornment))
				adornment.Measure(new(double.PositiveInfinity, availableSize.Height));

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
				var left = boundary - width / 2;
				if (adornment is ResizeVisual && Math.Abs(boundary - offset) < 0.5)
					left = boundary - width;
				adornment.Arrange(new(
					Math.Max(0, left),
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

		private void EnsureAdornments(IReadOnlyList<UIElement> contentChildren)
		{
			var columns = contentChildren
				.Select(GetColumn)
				.Where(column => column is not null)
				.Cast<TableViewColumn>()
				.ToList();
			var boundaryColumns = columns;
			var boundaryColumnSet = boundaryColumns.ToHashSet();

			foreach (var column in _resizeVisuals.Keys.Where(column => !boundaryColumnSet.Contains(column)).ToList())
				RemoveAdornments(column);

			foreach (var column in boundaryColumns)
			{
				if (_resizeVisuals.ContainsKey(column))
					continue;

				var resizeVisual = new ResizeVisual
				{
					Orientation = Orientation.Horizontal,
					Tag = column,
				};
				Canvas.SetZIndex(resizeVisual, ResizeVisualZIndex);
				resizeVisual.DragStarted += ResizeVisual_DragStarted;
				resizeVisual.DragDelta += ResizeVisual_DragDelta;
				resizeVisual.DragCompleted += ResizeVisual_DragCompleted;
				_resizeVisuals[column] = resizeVisual;

				var divider = new Rectangle { Tag = column };
				Canvas.SetZIndex(divider, DividerZIndex);
				_dividers[column] = divider;
			}

			var desiredAdornments = boundaryColumns
				.Select(column => (UIElement)_dividers[column])
				.Concat(boundaryColumns.Select(column => _resizeVisuals[column]))
				.ToList();
			var currentAdornments = Children.Where(IsAdornment).ToList();
			if (!currentAdornments.SequenceEqual(desiredAdornments))
			{
				foreach (var adornment in currentAdornments)
					Children.Remove(adornment);

				foreach (var adornment in desiredAdornments)
					Children.Add(adornment);
			}

			UpdateResizeVisualInteractionState();
		}

		private void RemoveAdornments(TableViewColumn column)
		{
			if (_resizeVisuals.Remove(column, out var resizeVisual))
			{
				resizeVisual.DragStarted -= ResizeVisual_DragStarted;
				resizeVisual.DragDelta -= ResizeVisual_DragDelta;
				resizeVisual.DragCompleted -= ResizeVisual_DragCompleted;
				Children.Remove(resizeVisual);
			}

			if (_dividers.Remove(column, out var divider))
				Children.Remove(divider);
		}

		internal void UpdateResizeVisualInteractionState()
		{
			foreach (var (column, resizeVisual) in _resizeVisuals)
			{
				var isEnabled = CanResizeColumns && column.CanUserResize;
				resizeVisual.IsEnabled = isEnabled;
				resizeVisual.IsHitTestVisible = isEnabled;
			}
		}

		private void ResizeVisual_DragStarted(object sender, DragStartedEventArgs e)
		{
			ColumnResizeStarted?.Invoke(sender, e);
		}

		private void ResizeVisual_DragDelta(object sender, DragDeltaEventArgs e)
		{
			ColumnResizeDelta?.Invoke(sender, e);
		}

		private void ResizeVisual_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			ColumnResizeCompleted?.Invoke(sender, e);
		}
	}
}
