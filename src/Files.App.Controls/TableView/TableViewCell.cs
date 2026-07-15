// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation.Peers;
using CommunityToolkit.WinUI;
using Windows.System;

namespace Files.App.Controls
{
	public partial class TableViewCell : ContentControl
	{
		private static readonly SolidColorBrush TransparentBackground = new(Microsoft.UI.Colors.Transparent);
		private object? _data;

		public TableViewColumn? Column { get; private set; }
		public object? Data => _data;
		public FrameworkElement? EditingElement => IsEditing ? Content as FrameworkElement : null;
		public bool IsEditing { get; private set; }

		public TableViewCell()
		{
			DefaultStyleKey = typeof(TableViewCell);
			Background = TransparentBackground;
			VerticalAlignment = VerticalAlignment.Stretch;
			HorizontalAlignment = HorizontalAlignment.Stretch;
			HorizontalContentAlignment = HorizontalAlignment.Stretch;
			VerticalContentAlignment = VerticalAlignment.Stretch;
			IsTabStop = true;

			AddHandler(PointerPressedEvent, new PointerEventHandler(TableViewCell_PointerPressed), true);
			DoubleTapped += TableViewCell_DoubleTapped;
			KeyDown += TableViewCell_KeyDown;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			UpdateValidationVisualState(false);
		}

		internal void Bind(TableViewColumn column, object dataItem)
		{
			EnsureEndEdit(TableViewEditEndingReason.RowRecycled);

			var existingElement = Content as FrameworkElement;
			Column = column;
			_data = dataItem;
			IsEditing = false;
			if (existingElement is null || !column.UpdateElement(existingElement, dataItem))
				Content = column.GenerateElement(dataItem);
		}

		internal void Refresh()
		{
			if (!IsEditing && Column is not null && _data is not null && Content is FrameworkElement element &&
				!Column.UpdateElement(element, _data))
			{
				Content = Column.GenerateElement(_data);
			}
		}

		internal void EnsureEndEdit(TableViewEditEndingReason reason)
		{
			if (IsEditing)
				CancelEdit(reason);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TableViewCellAutomationPeer(this);
		}

		public bool BeginEdit()
		{
			var owner = Column?.GetOwner();
			if (IsEditing || Column is null || _data is null || !Column.CanEdit(_data) ||
				Column.IsEffectivelyReadOnly ||
				owner is not null && (owner.IsReadOnly || !owner.TryBeginEdit(this)))
				return false;

			ValidationError = null;
			HasValidationError = false;
			var editingElement = Column.GenerateEditingElement(_data);
			Content = editingElement;
			IsEditing = true;
			Column.PrepareCellForEdit(this, editingElement);
			return true;
		}

		public bool CommitEdit()
		{
			return CommitEdit(TableViewEditEndingReason.Explicit);
		}

		internal bool CommitEdit(TableViewEditEndingReason reason)
		{
			if (!IsEditing || Column is null)
				return true;

			var owner = Column.GetOwner();
			if (owner is not null && !owner.RaiseCellEditEnding(this, TableViewEditAction.Commit, reason))
				return false;

			var result = Column.CommitCellEdit(this);
			if (!result.Succeeded)
			{
				ValidationError = result.ErrorContent;
				HasValidationError = true;
				owner?.RaiseCellEditFailed(this, ValidationError);
				return false;
			}

			ValidationError = null;
			HasValidationError = false;
			IsEditing = false;
			Content = _data is null ? null : Column.GenerateElement(_data);
			owner?.NotifyCellEditEnded(this);

			return true;
		}

		public void CancelEdit()
		{
			CancelEdit(TableViewEditEndingReason.Explicit);
		}

		internal bool CancelEdit(TableViewEditEndingReason reason)
		{
			if (!IsEditing || Column is null)
				return true;

			var owner = Column.GetOwner();
			if (owner is not null &&
				!owner.RaiseCellEditEnding(this, TableViewEditAction.Cancel, reason) &&
				!IsForcedEditEndingReason(reason))
				return false;

			Column.CancelCellEdit(this);
			ValidationError = null;
			HasValidationError = false;
			IsEditing = false;
			Content = _data is null ? null : Column.GenerateElement(_data);
			owner?.NotifyCellEditEnded(this);
			return true;
		}

		private static bool IsForcedEditEndingReason(TableViewEditEndingReason reason)
		{
			return reason is
				TableViewEditEndingReason.RowRecycled or
				TableViewEditEndingReason.ColumnRemoved or
				TableViewEditEndingReason.ControlUnloaded or
				TableViewEditEndingReason.ReadOnlyChanged;
		}

		private void UpdateValidationVisualState(bool useTransitions)
		{
			VisualStateManager.GoToState(this, HasValidationError ? "ValidationError" : "Valid", useTransitions);
		}

		private void TableViewCell_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (!IsEditing)
				Column?.GetOwner()?.CancelEdit(TableViewEditEndingReason.AnotherCellPressed);
		}

		private void TableViewCell_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (!IsEditing && BeginEdit())
				e.Handled = true;
		}

		private void TableViewCell_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (!IsEditing && e.Key is VirtualKey.F2 && BeginEdit())
			{
				e.Handled = true;
				return;
			}

			if (IsEditing || Column?.GetOwner() is not { } owner)
				return;

			var moved = e.Key switch
			{
				VirtualKey.Left => owner.TryMoveCellFocus(this, 0, FlowDirection is FlowDirection.RightToLeft ? 1 : -1),
				VirtualKey.Right => owner.TryMoveCellFocus(this, 0, FlowDirection is FlowDirection.RightToLeft ? -1 : 1),
				VirtualKey.Up => owner.TryMoveCellFocus(this, -1, 0),
				VirtualKey.Down => owner.TryMoveCellFocus(this, 1, 0),
				_ => false,
			};
			e.Handled = moved;
		}

		internal string GetAutomationValue()
		{
			var directValue = Content switch
			{
				TextBlock textBlock => textBlock.Text,
				TextBox textBox => textBox.Text,
				CheckBox checkBox => checkBox.IsChecked?.ToString() ?? string.Empty,
				_ => null,
			};
			if (directValue is not null)
				return directValue;

			if (Content is not FrameworkElement element)
				return string.Empty;

			var automationName = Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(element);
			if (!string.IsNullOrEmpty(automationName))
				return automationName;

			return element.FindDescendant<TextBox>()?.Text ??
				element.FindDescendant<TextBlock>()?.Text ??
				element.FindDescendant<CheckBox>()?.IsChecked?.ToString() ??
				string.Empty;
		}

		internal bool SetAutomationValue(string value)
		{
			if (!IsEditing && !BeginEdit())
				return false;

			var textBox = EditingElement as TextBox ?? EditingElement?.FindDescendant<TextBox>();
			if (textBox is null)
				return false;

			textBox.Text = value;
			if (EditingElement is ContentPresenter { Content: TableViewTemplateCellEditingContext context })
				context.Value = value;
			return CommitEdit();
		}
	}
}
