// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls;

public partial class TableView
{
	public event EventHandler<TableViewColumnSortingEventArgs>? Sorting;

	public event EventHandler<ReorderedItemsEventArgs>? ColumnReordered;

	public event EventHandler<TableViewBeginningEditEventArgs>? BeginningEdit;

	public event EventHandler<TableViewCellEditEndingEventArgs>? CellEditEnding;

	public event EventHandler<TableViewCellEditFailedEventArgs>? CellEditFailed;

	internal bool RaiseBeginningEdit(TableViewCell cell)
	{
		var args = new TableViewBeginningEditEventArgs(cell);
		BeginningEdit?.Invoke(this, args);
		return !args.Cancel;
	}

	internal bool RaiseCellEditEnding(TableViewCell cell, TableViewEditAction editAction, TableViewEditEndingReason reason)
	{
		var args = new TableViewCellEditEndingEventArgs(cell, editAction, reason);
		CellEditEnding?.Invoke(this, args);
		return !args.Cancel;
	}

	internal void RaiseCellEditFailed(TableViewCell cell, object? errorContent)
	{
		CellEditFailed?.Invoke(this, new(cell, errorContent));
	}
}

public enum TableViewEditAction
{
	Commit,
	Cancel,
}

public enum TableViewEditEndingReason
{
	Explicit,
	FocusLost,
	AnotherCellPressed,
	RowRecycled,
	ColumnRemoved,
	ControlUnloaded,
	ReadOnlyChanged,
	ColumnOperation,
}

public sealed class TableViewBeginningEditEventArgs : EventArgs
{
	public TableViewBeginningEditEventArgs(TableViewCell cell)
	{
		Cell = cell;
	}

	public TableViewCell Cell { get; }

	public bool Cancel { get; set; }
}

public sealed class TableViewCellEditEndingEventArgs : EventArgs
{
	public TableViewCellEditEndingEventArgs(TableViewCell cell, TableViewEditAction editAction, TableViewEditEndingReason reason)
	{
		Cell = cell;
		EditAction = editAction;
		Reason = reason;
	}

	public TableViewCell Cell { get; }

	public TableViewEditAction EditAction { get; }

	public TableViewEditEndingReason Reason { get; }

	public bool Cancel { get; set; }
}

public sealed class TableViewCellEditFailedEventArgs : EventArgs
{
	public TableViewCellEditFailedEventArgs(TableViewCell cell, object? errorContent)
	{
		Cell = cell;
		ErrorContent = errorContent;
	}

	public TableViewCell Cell { get; }

	public object? ErrorContent { get; }
}

public sealed class TableViewColumnSortingEventArgs : EventArgs
{
	public TableViewColumnSortingEventArgs(TableViewColumn column, ListSortDirection sortDirection)
	{
		Column = column;
		SortDirection = sortDirection;
	}

	public TableViewColumn Column { get; }

	public ListSortDirection SortDirection { get; set; }

	public bool Cancel { get; set; }
}
