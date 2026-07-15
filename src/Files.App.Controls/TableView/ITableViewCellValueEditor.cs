// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public interface ITableViewCellValueEditor
	{
		public TableViewCellEditResult TrySetValue(string name, object? value);
	}
}
