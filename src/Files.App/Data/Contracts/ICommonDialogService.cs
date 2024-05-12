// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	public interface ICommonDialogService
	{
		string Open_FileOpenDialog(nint hWnd, string[] filters);
	}
}
