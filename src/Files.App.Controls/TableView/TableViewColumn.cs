// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Controls
{
	public abstract partial class TableViewColumn : Control
	{
		private const string TemplatePartName_RootGrid = "PART_RootGrid";
		private const string TemplatePartName_ColumnSizer = "PART_ColumnSizer";

		private const string TemplateVisualStateName_Normal = "Normal";
		private const string TemplateVisualStateName_PointerOver = "PointerOver";
		private const string TemplateVisualStateName_Pressed = "Pressed";

		private WeakReference<TableView>? _owner;

		private Grid? _rootGrid;
		ContentSizer? _columnSizer;

		[GeneratedDependencyProperty]
		public partial string? Header { get; set; }

		[GeneratedDependencyProperty]
		public partial string? Binding { get; set; }

		public TableViewColumn()
		{
			DefaultStyleKey = typeof(TableViewColumn);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_rootGrid = GetTemplateChild(TemplatePartName_RootGrid) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_RootGrid} in the given {nameof(TableViewColumn)}'s style.");
			_columnSizer = GetTemplateChild(TemplatePartName_ColumnSizer) as ContentSizer
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ColumnSizer} in the given {nameof(TableView)}'s style.");

			_rootGrid.PointerEntered += RootGrid_PointerEntered;
			_rootGrid.PointerExited += RootGrid_PointerExited;
			_rootGrid.PointerPressed += RootGrid_PointerPressed;
			_rootGrid.PointerReleased += RootGrid_PointerReleased;

			_columnSizer.ManipulationDelta += ColumnSizer_ManipulationDelta;
			_columnSizer.ManipulationCompleted += ColumnSizer_ManipulationCompleted;
		}

		public abstract FrameworkElement BuildCellElement(object dataItem);

		public abstract FrameworkElement BuildEditCellElement(object dataItem);

		public abstract void ApplyStyle(Style style);

		public void SetOwner(TableView owner)
		{
			_owner = new(owner);
		}

		public void ResetPointerEventVisual()
		{
			VisualStateManager.GoToState(this, TemplateVisualStateName_Normal, true);
		}

		private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (_owner is not null && _owner.TryGetTarget(out var owner) && owner.IsColumnResizing)
			{
				ResetPointerEventVisual();
				return;
			}

			VisualStateManager.GoToState(this, TemplateVisualStateName_PointerOver, true);
		}

		private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			ResetPointerEventVisual();
		}

		private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, TemplateVisualStateName_Pressed, true);
		}

		private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, TemplateVisualStateName_PointerOver, true);
		}

		private void ColumnSizer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (_owner is null || !_owner.TryGetTarget(out var owner))
				return;

			if (!owner.IsColumnResizing)
			{
				ResetPointerEventVisual();
				owner.IsColumnResizing = true;
			}

			owner.RearrangeRows();
		}

		private void ColumnSizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			if (_owner is null || !_owner.TryGetTarget(out var owner))
				return;

			ResetPointerEventVisual();
			owner.IsColumnResizing = false;
			owner.RearrangeRows();
		}
	}
}
