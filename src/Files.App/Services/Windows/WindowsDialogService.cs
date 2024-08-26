// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WNet;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Services
{
	/// <inheritdoc cref="ICommonDialogService"/>
	public sealed class CommonDialogService : ICommonDialogService
	{
		/// <inheritdoc/>
		public bool Open_FileOpenDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath)
		{
			filePath = string.Empty;

			try
			{
				unsafe
				{
					// Get a new instance of the dialog
					PInvoke.CoCreateInstance(
						typeof(FileOpenDialog).GUID,
						null,
						CLSCTX.CLSCTX_INPROC_SERVER,
						out IFileOpenDialog* pDialog)
					.ThrowOnFailure();

					if (filters.Length is not 0 && filters.Length % 2 is 0)
					{
						List<COMDLG_FILTERSPEC> extensions = [];

						for (int i = 1; i < filters.Length; i += 2)
						{
							COMDLG_FILTERSPEC extension;

							extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filters[i]);
							extension.pszName = (char*)Marshal.StringToHGlobalUni(filters[i - 1]);

							// Add to the exclusive extension list
							extensions.Add(extension);
						}

						// Set the file type using the extension list
						pDialog->SetFileTypes(extensions.ToArray());
					}

					// Get the default shell folder (My Computer)
					PInvoke.SHCreateItemFromParsingName(
						Environment.GetFolderPath(defaultFolder),
						null,
						typeof(IShellItem).GUID,
						out var directoryShellItem)
					.ThrowOnFailure();

					// Folder picker
					if (pickFoldersOnly)
					{
						pDialog->SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);
					}

					// Set the default folder to open in the dialog
					pDialog->SetFolder((IShellItem*)directoryShellItem);
					pDialog->SetDefaultFolder((IShellItem*)directoryShellItem);

					// Show the dialog
					pDialog->Show(new HWND(hWnd));

					// Get the file that user chose
					IShellItem* resultShellItem = default;
					pDialog->GetResult(&resultShellItem);
					resultShellItem->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
					filePath = lpFilePath.ToString();

					return true;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, "Failed to open a common dialog called FileOpenDialog.");

				return false;
			}
		}

		/// <inheritdoc/>
		public bool Open_FileSaveDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath)
		{
			filePath = string.Empty;

			try
			{
				unsafe
				{
					// Get a new instance of the dialog
					PInvoke.CoCreateInstance(
						typeof(FileSaveDialog).GUID,
						null,
						CLSCTX.CLSCTX_INPROC_SERVER,
						out IFileSaveDialog* pDialog)
					.ThrowOnFailure();

					if (filters.Length is not 0 && filters.Length % 2 is 0)
					{
						List<COMDLG_FILTERSPEC> extensions = [];

						for (int i = 1; i < filters.Length; i += 2)
						{
							COMDLG_FILTERSPEC extension;

							extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filters[i]);
							extension.pszName = (char*)Marshal.StringToHGlobalUni(filters[i - 1]);

							// Add to the exclusive extension list
							extensions.Add(extension);
						}

						// Set the file type using the extension list
						pDialog->SetFileTypes(extensions.ToArray());
					}

					// Get the default shell folder (My Computer)
					PInvoke.SHCreateItemFromParsingName(
						Environment.GetFolderPath(defaultFolder),
						null,
						typeof(IShellItem).GUID,
						out var directoryShellItem)
					.ThrowOnFailure();

					// Folder picker
					if (pickFoldersOnly)
						pDialog->SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

					// Set the default folder to open in the dialog
					pDialog->SetFolder((IShellItem*)directoryShellItem);
					pDialog->SetDefaultFolder((IShellItem*)directoryShellItem);

					// Show the dialog
					pDialog->Show(new HWND(hWnd));

					// Get the file that user chose
					IShellItem* resultShellItem = default;
					pDialog->GetResult(&resultShellItem);
					resultShellItem->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
					filePath = lpFilePath.ToString();

					return true;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, "Failed to open a common dialog called FileSaveDialog.");

				return false;
			}
		}

		/// <inheritdoc/>
		public unsafe bool Open_NetworkConnectionDialog(nint hWind, bool hideRestoreConnectionCheckBox = false, bool persistConnectionAtLogon = false, bool readOnlyPath = false, string? remoteNetworkName = null, bool useMostRecentPath = false)
		{
			NETRESOURCEW netRes = default;
			CONNECTDLGSTRUCTW dialogOptions = default;

			dialogOptions.cbStructure = (uint)Marshal.SizeOf(typeof(CONNECTDLGSTRUCTW));
			netRes.dwType = NET_RESOURCE_TYPE.RESOURCETYPE_DISK;

			if (hideRestoreConnectionCheckBox)
				dialogOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_HIDE_BOX;
			else
				dialogOptions.dwFlags &= ~CONNECTDLGSTRUCT_FLAGS.CONNDLG_HIDE_BOX;

			if (persistConnectionAtLogon)
			{
				dialogOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_PERSIST;
				dialogOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_NOT_PERSIST;
			}
			else
			{
				dialogOptions.dwFlags &= ~CONNECTDLGSTRUCT_FLAGS.CONNDLG_PERSIST;
				dialogOptions.dwFlags &= ~CONNECTDLGSTRUCT_FLAGS.CONNDLG_NOT_PERSIST;
			}

			fixed (char* lpcRemoteName = remoteNetworkName)
				netRes.lpRemoteName = lpcRemoteName;

			if (useMostRecentPath && !string.IsNullOrEmpty(remoteNetworkName))
				throw new InvalidOperationException($"{nameof(useMostRecentPath)} cannot be set to true if {nameof(remoteNetworkName)} has a value.");

			if (useMostRecentPath)
				dialogOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_USE_MRU;
			else
				dialogOptions.dwFlags &= ~CONNECTDLGSTRUCT_FLAGS.CONNDLG_USE_MRU;

			dialogOptions.hwndOwner = new(hWind);

			dialogOptions.lpConnRes = &netRes;

			if (readOnlyPath && !string.IsNullOrEmpty(netRes.lpRemoteName.ToString()))
				dialogOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_RO_PATH;

			var result = PInvoke.WNetConnectionDialog1W(ref dialogOptions);

			dialogOptions.lpConnRes = null;

			if ((uint)result == unchecked((uint)-1))
				return false;

			if (result == 0)
				throw new Win32Exception("Cannot display dialog");

			return true;
		}
	}
}
