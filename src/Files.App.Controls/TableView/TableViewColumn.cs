// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Files.App.Controls
{
	public abstract partial class TableViewColumn : Control
	{
		private WeakReference<TableView>? _owner;
		private Grid? _rootGrid;

		public TableViewColumn()
		{
			DefaultStyleKey = typeof(TableViewColumn);
			IsTabStop = true;
			IsEnabledChanged += TableViewColumn_IsEnabledChanged;
			KeyDown += TableViewColumn_KeyDown;
			SizeChanged += TableViewColumn_SizeChanged;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UnhookRootGrid();
			_rootGrid = GetTemplateChild(TemplatePartName_RootGrid) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_RootGrid} in the given {nameof(TableViewColumn)}'s style.");

			_rootGrid.PointerEntered += RootGrid_PointerEntered;
			_rootGrid.PointerExited += RootGrid_PointerExited;
			_rootGrid.PointerPressed += RootGrid_PointerPressed;
			_rootGrid.PointerReleased += RootGrid_PointerReleased;
			_rootGrid.Tapped += RootGrid_Tapped;
			UpdateSortVisualState(false);
			UpdateEnabledVisualState(false);
			AutomationProperties.SetName(this, Header ?? string.Empty);
		}

		public abstract FrameworkElement GenerateElement(object dataItem);

		public virtual FrameworkElement GenerateEditingElement(object dataItem)
		{
			throw new NotSupportedException($"{GetType().Name} does not support editing.");
		}

		protected internal abstract bool UpdateElement(FrameworkElement element, object dataItem);

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

		internal void AttachOwner(TableView owner)
		{
			VerifyCanAttachOwner(owner);

			_owner = new(owner);
			owner.RegisterInitialSortColumn(this);
		}

		internal void VerifyCanAttachOwner(TableView owner)
		{
			if (_owner is not null && _owner.TryGetTarget(out var currentOwner) && currentOwner != owner)
				throw new InvalidOperationException($"A {nameof(TableViewColumn)} cannot be shared by multiple {nameof(TableView)} controls.");
		}

		internal void NotifySortDirectionChanged()
		{
			if (_owner is null || !_owner.TryGetTarget(out var owner))
				return;

			if (SortDirection is null)
				owner.UnregisterSortColumn(this);
			else
				owner.RegisterInitialSortColumn(this);
		}

		internal void DetachOwner(TableView owner)
		{
			if (_owner is null || !_owner.TryGetTarget(out var currentOwner) || currentOwner != owner)
				return;

			owner.UnregisterSortColumn(this);
			_owner = null;
		}

		internal void RequestSort()
		{
			if (IsEnabled && _owner is not null && _owner.TryGetTarget(out var owner) && !owner.IsColumnResizing)
				owner.RequestSort(this);
		}

		internal TableView? GetOwner()
		{
			return _owner is not null && _owner.TryGetTarget(out var owner) ? owner : null;
		}

		internal protected void ResetPointerEventVisual()
		{
			UpdateEnabledVisualState(true);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TableViewColumnAutomationPeer(this);
		}

		private void UnhookRootGrid()
		{
			if (_rootGrid is null)
				return;

			_rootGrid.PointerEntered -= RootGrid_PointerEntered;
			_rootGrid.PointerExited -= RootGrid_PointerExited;
			_rootGrid.PointerPressed -= RootGrid_PointerPressed;
			_rootGrid.PointerReleased -= RootGrid_PointerReleased;
			_rootGrid.Tapped -= RootGrid_Tapped;
			_rootGrid = null;
		}

		private void UpdateSortVisualState(bool useTransitions)
		{
			var visualStateName = SortDirection switch
			{
				ListSortDirection.Ascending => TemplateVisualStateName_SortOrderAscending,
				ListSortDirection.Descending => TemplateVisualStateName_SortOrderDescending,
				_ => TemplateVisualStateName_SortOrderNone,
			};
			VisualStateManager.GoToState(this, visualStateName, useTransitions);
			AutomationProperties.SetItemStatus(this, SortDirection?.ToString() ?? string.Empty);
		}

		private void UpdateEnabledVisualState(bool useTransitions)
		{
			VisualStateManager.GoToState(
				this,
				IsEnabled ? TemplateVisualStateName_ColumnNormal : TemplateVisualStateName_ColumnDisabled,
				useTransitions);
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
