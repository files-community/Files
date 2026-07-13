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
		private TextBlock? _headerTextBlock;
		private FontIcon? _sortOrderGlyph;
		private bool _isApplyingResolvedWidth;
		private double _autoDesiredWidth;

		public TableViewColumn()
		{
			DefaultStyleKey = typeof(TableViewColumn);
			IsTabStop = true;
			RegisterPropertyChangedCallback(WidthProperty, OnWidthPropertyChanged);
			RegisterPropertyChangedCallback(MinWidthProperty, OnColumnSizeConstraintChanged);
			RegisterPropertyChangedCallback(MaxWidthProperty, OnColumnSizeConstraintChanged);
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
			_headerTextBlock = GetTemplateChild(TemplatePartName_HeaderTextBlock) as TextBlock;
			_sortOrderGlyph = GetTemplateChild(TemplatePartName_SortOrderGlyph) as FontIcon;

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

		protected internal virtual TableViewCellEditResult CommitCellEdit(TableViewCell cell)
		{
			return TableViewCellEditResult.Success;
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
			if (IsEnabled && IsSortingEnabled && _owner is not null && _owner.TryGetTarget(out var owner) && !owner.IsColumnResizing)
				owner.RequestSort(this);
		}

		internal TableView? GetOwner()
		{
			return _owner is not null && _owner.TryGetTarget(out var owner) ? owner : null;
		}

		internal bool IsEffectivelyReadOnly => IsReadOnly || GetOwner() is { IsReadOnly: true };

		internal void ApplyResolvedWidth(double width)
		{
			_isApplyingResolvedWidth = true;
			try
			{
				Width = width;
			}
			finally
			{
				_isApplyingResolvedWidth = false;
			}
		}

		internal void NotifyColumnWidthChanged()
		{
			if (ColumnWidth.IsAuto)
				_autoDesiredWidth = 0;

			NotifyPropertyChanged(
				TableViewNotificationTarget.ColumnLayout |
				TableViewNotificationTarget.RowLayout |
				TableViewNotificationTarget.ResizeVisuals);
		}

		internal double AutoDesiredWidth => Math.Clamp(Math.Max(MinWidth, _autoDesiredWidth), MinWidth, MaxWidth);

		internal bool ReportAutoDesiredWidth(double desiredWidth)
		{
			if (!ColumnWidth.IsAuto || double.IsNaN(desiredWidth) || double.IsInfinity(desiredWidth))
				return false;

			desiredWidth = Math.Clamp(desiredWidth, MinWidth, MaxWidth);
			if (desiredWidth <= _autoDesiredWidth)
				return false;

			_autoDesiredWidth = desiredWidth;
			return true;
		}

		internal bool MeasureHeaderDesiredWidth(double availableHeight)
		{
			if (!ColumnWidth.IsAuto || _headerTextBlock is null)
				return false;

			_headerTextBlock.Measure(new(double.PositiveInfinity, availableHeight));
			double desiredWidth = _headerTextBlock.DesiredSize.Width;
			if (_sortOrderGlyph is { Visibility: Visibility.Visible })
			{
				_sortOrderGlyph.Measure(new(double.PositiveInfinity, availableHeight));
				desiredWidth += _sortOrderGlyph.DesiredSize.Width;
			}

			return ReportAutoDesiredWidth(desiredWidth);
		}

		private void NotifyInteractionOptionsChanged()
		{
			NotifyPropertyChanged(TableViewNotificationTarget.ResizeVisuals);
		}

		private protected void NotifyPropertyChanged(TableViewNotificationTarget target)
		{
			if (_owner is not null && _owner.TryGetTarget(out var owner))
				owner.NotifyPropertyChanged(this, target);
		}

		private bool IsSortingEnabled =>
			CanUserSort &&
			_owner is not null &&
			_owner.TryGetTarget(out var owner) &&
			owner.CanUserSortColumns;

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
			_headerTextBlock = null;
			_sortOrderGlyph = null;
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

		private void OnWidthPropertyChanged(DependencyObject sender, DependencyProperty dp)
		{
			if (_isApplyingResolvedWidth ||
				double.IsNaN(Width))
			{
				return;
			}

			throw new InvalidOperationException(
				$"{nameof(TableViewColumn)}.{nameof(Width)} is reserved for internal layout. Use {nameof(ColumnWidth)} instead.");
		}

		internal void ResetAutoDesiredWidth()
		{
			if (ColumnWidth.IsAuto)
				_autoDesiredWidth = 0;
		}

		private void OnColumnSizeConstraintChanged(DependencyObject sender, DependencyProperty dp)
		{
			NotifyColumnWidthChanged();
		}
	}
}
