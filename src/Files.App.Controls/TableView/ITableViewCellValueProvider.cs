// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public interface ITableViewCellValueProvider
	{
		public object? GetValue(string name);
	}
}
