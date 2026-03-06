// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public abstract partial class TableViewColumn : Control
	{
		private WeakReference<TableView>? _owner;
		private Grid? _rootGrid;

		public TimeSpan EditDoubleClickInterval { get; set; } = TimeSpan.FromMilliseconds(1500);

		public TableViewColumn()
		{
			DefaultStyleKey = typeof(TableViewColumn);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_rootGrid = GetTemplateChild(TemplatePartName_RootGrid) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_RootGrid} in the given {nameof(TableViewColumn)}'s style.");

			Loaded += TableViewColumn_Loaded;

			_rootGrid.PointerEntered += RootGrid_PointerEntered;
			_rootGrid.PointerExited += RootGrid_PointerExited;
			_rootGrid.PointerPressed += RootGrid_PointerPressed;
			_rootGrid.PointerReleased += RootGrid_PointerReleased;
		}

		public abstract FrameworkElement GenerateElement(object dataItem);

		public abstract FrameworkElement GenerateEditingElement(object dataItem);

		protected internal virtual bool CanEdit(object dataItem)
		{
			return false;
		}

		protected internal virtual void PrepareCellForEdit(TableViewCell cell, FrameworkElement editingElement)
		{
		}

		protected internal virtual bool CommitCellEdit(TableViewCell cell)
		{
			return true;
		}

		protected internal virtual void CancelCellEdit(TableViewCell cell)
		{
		}

		public void EnsureOwner(TableView owner)
		{
			_owner ??= new(owner);
		}

		internal protected void ResetPointerEventVisual()
		{
			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnNormal, true);
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

			owner.InvalidateLayoutOfAllRows();
		}

		internal protected void OnColumnResizeCompleted()
		{
			if (_owner is null || !_owner.TryGetTarget(out var owner))
				return;

			ResetPointerEventVisual();
			owner.IsColumnResizing = false;
			owner.InvalidateLayoutOfAllRows();
		}
	}
}
