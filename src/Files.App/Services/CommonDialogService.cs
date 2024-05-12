// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Services
{
	public class CommonDialogService : ICommonDialogService
	{
		public string Open_FileOpenDialog(nint hWnd, string[] filters)
		{
			try
			{
				unsafe
				{
					// Get a new instance of the OpenFileDialog
					PInvoke.CoCreateInstance(
						typeof(FileOpenDialog).GUID,
						null,
						CLSCTX.CLSCTX_INPROC_SERVER,
						out IFileOpenDialog openDialog)
					.ThrowOnFailure();

					List<COMDLG_FILTERSPEC> extensions = [];

					if (filters.Length is not 0 && filters.Length % 2 is 0)
					{
						// All even numbered tokens should be labels.
						// Odd numbered tokens are the associated extensions.
						for (int i = 1; i < filters.Length; i += 2)
						{
							COMDLG_FILTERSPEC extension;

							extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filters[i]);
							extension.pszName = (char*)Marshal.StringToHGlobalUni(filters[i - 1]);

							// Add to the exclusive extension list
							extensions.Add(extension);
						}
					}

					// Set the file type using the extension list
					openDialog.SetFileTypes(extensions.ToArray());

					// Get the default shell folder (My Computer)
					PInvoke.SHCreateItemFromParsingName(
						Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
						null,
						typeof(IShellItem).GUID,
						out var directoryShellItem)
					.ThrowOnFailure();

					// Set the default folder to open in the dialog
					openDialog.SetFolder((IShellItem)directoryShellItem);
					openDialog.SetDefaultFolder((IShellItem)directoryShellItem);

					// Show the dialog
					openDialog.Show(new HWND(hWnd));

					// Get the file that user chose
					openDialog.GetResult(out var resultShellItem);
					resultShellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
					var filePath = lpFilePath.ToString();

					return filePath;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, "Failed to open a common dialog called OpenFileDialog.");

				return string.Empty;
			}
		}
	}
}
