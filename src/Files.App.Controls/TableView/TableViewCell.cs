// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
			EnsureEndEdit();

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

		internal void EnsureEndEdit()
		{
			if (!IsEditing)
				return;

			if (!CommitEdit())
				CancelEdit();
		}

		public bool BeginEdit()
		{
			if (IsEditing || Column is null || _data is null || !Column.CanEdit(_data) ||
				Column.IsEffectivelyReadOnly ||
				Column.GetOwner() is { } owner && (owner.IsReadOnly || !owner.RaiseBeginningEdit(this)))
				return false;

			HasValidationError = false;
			var editingElement = Column.GenerateEditingElement(_data);
			IsEditing = true;
			Column.PrepareCellForEdit(this, editingElement);
			Content = editingElement;
			return true;
		}

		public bool CommitEdit()
		{
			if (!IsEditing || Column is null)
				return true;

			if (Column.GetOwner() is { } owner && !owner.RaiseCellEditEnding(this, TableViewEditAction.Commit))
				return false;

			if (!Column.CommitCellEdit(this))
			{
				HasValidationError = true;
				return false;
			}

			HasValidationError = false;
			IsEditing = false;
			Content = _data is null ? null : Column.GenerateElement(_data);

			return true;
		}

		public void CancelEdit()
		{
			if (!IsEditing || Column is null ||
				Column.GetOwner() is { } owner && !owner.RaiseCellEditEnding(this, TableViewEditAction.Cancel))
				return;

			Column.CancelCellEdit(this);
			HasValidationError = false;
			IsEditing = false;
			Content = _data is null ? null : Column.GenerateElement(_data);
		}

		private void UpdateValidationVisualState(bool useTransitions)
		{
			VisualStateManager.GoToState(this, HasValidationError ? "ValidationError" : "Valid", useTransitions);
		}

		private void TableViewCell_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (!CanBeginEdit())
				return;

			e.Handled = true;
			QueueBeginEdit();
		}

		private void TableViewCell_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key is not VirtualKey.F2 || !CanBeginEdit())
				return;

			e.Handled = true;
			QueueBeginEdit();
		}

		private bool CanBeginEdit()
		{
			return !IsEditing &&
				Column is not null &&
				_data is not null &&
				Column.CanEdit(_data) &&
				!Column.IsEffectivelyReadOnly;
		}

		private void QueueBeginEdit()
		{
			var column = Column;
			var dataItem = _data;
			DispatcherQueue.TryEnqueue(() =>
			{
				if (!CanBeginEdit() || Column != column || !ReferenceEquals(_data, dataItem))
					return;

				var owner = column?.GetOwner();
				if (owner is null || owner.CommitEdit())
					BeginEdit();
			});
		}
	}
}
