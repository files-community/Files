// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls;

public partial class TableView
{
	public event EventHandler<TableViewColumnSortingEventArgs>? Sorting;

	public event EventHandler<ReorderedItemsEventArgs>? ColumnReordered;

	public event EventHandler<TableViewBeginningEditEventArgs>? BeginningEdit;

	public event EventHandler<TableViewCellEditEndingEventArgs>? CellEditEnding;

	internal bool RaiseBeginningEdit(TableViewCell cell)
	{
		var args = new TableViewBeginningEditEventArgs(cell);
		BeginningEdit?.Invoke(this, args);
		return !args.Cancel;
	}

	internal bool RaiseCellEditEnding(TableViewCell cell, TableViewEditAction editAction)
	{
		var args = new TableViewCellEditEndingEventArgs(cell, editAction);
		CellEditEnding?.Invoke(this, args);
		return !args.Cancel;
	}
}

public enum TableViewEditAction
{
	Commit,
	Cancel,
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
	public TableViewCellEditEndingEventArgs(TableViewCell cell, TableViewEditAction editAction)
	{
		Cell = cell;
		EditAction = editAction;
	}

	public TableViewCell Cell { get; }

	public TableViewEditAction EditAction { get; }

	public bool Cancel { get; set; }
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
