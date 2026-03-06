// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
	public partial class TableViewCell : ContentControl
	{
		private static readonly SolidColorBrush TransparentBackground = new(Microsoft.UI.Colors.Transparent);
		private object? _data;
		private DateTimeOffset _lastClickTimestamp;

		internal TableViewColumn? Column { get; private set; }
		internal object? Data => _data;
		internal FrameworkElement? EditingElement => IsEditing ? Content as FrameworkElement : null;
		internal bool IsEditing { get; private set; }

		public TableViewCell()
		{
			Background = TransparentBackground;
			VerticalAlignment = VerticalAlignment.Stretch;
			HorizontalAlignment = HorizontalAlignment.Stretch;
			HorizontalContentAlignment = HorizontalAlignment.Stretch;
			VerticalContentAlignment = VerticalAlignment.Stretch;
			IsTabStop = false;

			PointerReleased += TableViewCell_PointerReleased;
		}

		internal void Bind(TableViewColumn column, ITableViewCellValueProvider dataItem)
		{
			EnsureEndEdit();

			Column = column;
			_data = dataItem;
			IsEditing = false;
			_lastClickTimestamp = default;
			Content = column.GenerateElement(dataItem);
		}

		internal void EnsureEndEdit()
		{
			if (!IsEditing)
				return;

			if (!CommitEdit())
				CancelEdit();
		}

		internal bool BeginEdit()
		{
			if (IsEditing || Column is null || _data is null || !Column.CanEdit(_data))
				return false;

			var editingElement = Column.GenerateEditingElement(_data);
			Content = editingElement;
			IsEditing = true;
			_lastClickTimestamp = default;
			Column.PrepareCellForEdit(this, editingElement);
			return true;
		}

		internal bool CommitEdit()
		{
			if (!IsEditing || Column is null || !Column.CommitCellEdit(this))
				return false;

			// End edit
			IsEditing = false;
			_lastClickTimestamp = default;
			Content = _data is null ? null : Column.GenerateElement(_data);

			return true;
		}

		internal void CancelEdit()
		{
			if (!IsEditing || Column is null)
				return;

			Column.CancelCellEdit(this);
			IsEditing = false;
			_lastClickTimestamp = default;
			Content = _data is null ? null : Column.GenerateElement(_data);
		}

		private void TableViewCell_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (Column is null ||
				_data is null ||
				IsEditing ||
				!Column.CanEdit(_data) ||
				e.Pointer.PointerDeviceType is not PointerDeviceType.Mouse ||
				e.GetCurrentPoint(this).Properties.PointerUpdateKind is not PointerUpdateKind.LeftButtonReleased)
				return;

			var now = DateTimeOffset.UtcNow;
			if (_lastClickTimestamp != default &&
				now - _lastClickTimestamp <= Column.EditDoubleClickInterval)
			{
				_lastClickTimestamp = default;

				if (BeginEdit())
					e.Handled = true;

				return;
			}

			_lastClickTimestamp = now;
		}
	}
}
