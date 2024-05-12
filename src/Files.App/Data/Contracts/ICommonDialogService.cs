// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides service to launch common dialog through Win32API.
	/// </summary>
	public interface ICommonDialogService
	{
		/// <summary>
		/// Opens a common dialog called FileOpenDialog through native Win32API.
		/// </summary>
		/// <param name="hWnd">The Window handle that the dialog launches based on.</param>
		/// <param name="filters">The extension filters that the dialog uses to exclude unnecessary files.<br/>The filter must have a pair:<code>[ "Application", ".exe" ]</code></param>
		/// <remarks>
		/// There's a WinRT API to launch this dialog, but the API doesn't support windows that are launched by those who is in Administrators group or has broader privileges.
		/// </remarks>
		/// <returns>The file path that user chose.</returns>
		string Open_FileOpenDialog(nint hWnd, string[] filters);
	}
}
