// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public interface ITableViewCellValueEditor
	{
		public bool TrySetValue<T>(string name, T value);
	}
}
