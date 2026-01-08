// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public abstract partial class TableViewColumn : Control
	{
		private WeakReference<TableView>? _owner;
		private bool _pointerEnteredToColumnVisual;
		private bool _pointerEnteredToFilterButtonVisual;

		private Grid? _rootGrid;
		private Border? _columnVisualBorder;
		private Border? _filterVisualBorder;

		public TableViewColumn()
		{
			DefaultStyleKey = typeof(TableViewColumn);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_rootGrid = GetTemplateChild(TemplatePartName_RootGrid) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_RootGrid} in the given {nameof(TableViewColumn)}'s style.");
			_columnVisualBorder = GetTemplateChild(TemplatePartName_ColumnVisualBorder) as Border
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ColumnVisualBorder} in the given {nameof(TableViewColumn)}'s style.");
			_filterVisualBorder = GetTemplateChild(TemplatePartName_FilterVisualBorder) as Border
				?? throw new MissingFieldException($"Could not find {TemplatePartName_FilterVisualBorder} in the given {nameof(TableViewColumn)}'s style.");

			Loaded += TableViewColumn_Loaded;

			_rootGrid.PointerEntered += RootGrid_PointerEntered;
			_rootGrid.PointerExited += RootGrid_PointerExited;
			_columnVisualBorder.PointerEntered += ColumnVisualBorder_PointerEntered;
			_columnVisualBorder.PointerExited += ColumnVisualBorder_PointerExited;
			_columnVisualBorder.PointerPressed += ColumnVisualBorder_PointerPressed;
			_columnVisualBorder.PointerReleased += ColumnVisualBorder_PointerReleased;
			_filterVisualBorder.PointerEntered += FilterBorder_PointerEntered;
			_filterVisualBorder.PointerExited += FilterBorder_PointerExited;
			_filterVisualBorder.PointerPressed += FilterBorder_PointerPressed;
			_filterVisualBorder.PointerReleased += FilterBorder_PointerReleased;
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
			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnNormal, true);
			VisualStateManager.GoToState(this, TemplateVisualStateName_FilterNormal, true);
		}

		internal protected void OnColumnBeingResized()
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

		internal protected void OnColumnResizeCompleted()
		{
			if (_owner is null || !_owner.TryGetTarget(out var owner))
				return;

			ResetPointerEventVisual();
			owner.IsColumnResizing = false;
			owner.RearrangeRows();
		}
	}
}
