﻿// Copyright (c) Files Community
// Licensed under the MIT License.

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
		public unsafe bool Open_FileOpenDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath)
		{
			filePath = string.Empty;

			try
			{
				using ComPtr<IFileOpenDialog> pDialog = default;
				HRESULT hr = pDialog.CoCreateInstance(CLSID.CLSID_FileOpenDialog, null, CLSCTX.CLSCTX_INPROC_SERVER);
				
				// Handle COM creation failure gracefully
				if (hr.Failed)
				{
					App.Logger.LogError("Failed to create IFileOpenDialog COM object. HRESULT: 0x{0:X8}", hr.Value);
					return false;
				}

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
					pDialog.Get()->SetFileTypes(extensions.ToArray());
				}

				// Get the default shell folder (My Computer)
				using ComPtr<IShellItem> pDefaultFolderShellItem = default;
				fixed (char* pszDefaultFolderPath = Environment.GetFolderPath(defaultFolder))
				{
					hr = PInvoke.SHCreateItemFromParsingName(
						pszDefaultFolderPath,
						null,
						IID.IID_IShellItem,
						(void**)pDefaultFolderShellItem.GetAddressOf());
					
					// Handle shell item creation failure gracefully
					if (hr.Failed)
					{
						App.Logger.LogWarning("Failed to create shell item for default folder '{0}'. HRESULT: 0x{1:X8}. Dialog will open without default folder.", Environment.GetFolderPath(defaultFolder), hr.Value);
						// Continue without setting default folder rather than failing completely
					}
				}

				// Folder picker
				if (pickFoldersOnly)
					pDialog.Get()->SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

				// Set the default folder to open in the dialog (only if creation succeeded)
				if (pDefaultFolderShellItem.Get() is not null)
				{
					pDialog.Get()->SetFolder(pDefaultFolderShellItem.Get());
					pDialog.Get()->SetDefaultFolder(pDefaultFolderShellItem.Get());
				}

				// Show the dialog
				hr = pDialog.Get()->Show(new HWND(hWnd));
				if (hr.Value == unchecked((int)0x800704C7)) // HRESULT_FROM_WIN32(ERROR_CANCELLED)
					return false;

				// Handle dialog show failure gracefully
				if (hr.Failed)
				{
					App.Logger.LogError("Failed to show FileSaveDialog. HRESULT: 0x{0:X8}", hr.Value);
					return false;
				}

				// Get the file that user chose
				using ComPtr<IShellItem> pResultShellItem = default;
				pDialog.Get()->GetResult(pResultShellItem.GetAddressOf());
				if (pResultShellItem.Get() is null)
					throw new COMException("FileOpenDialog returned invalid shell item.");
				pResultShellItem.Get()->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
				filePath = lpFilePath.ToString();

				return true;
			}
			catch (COMException comEx)
			{
				App.Logger.LogError(comEx, "COM failure while opening FileOpenDialog. HRESULT: 0x{0:X8}", comEx.HResult);
				return false;
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, "Unexpected error while opening FileOpenDialog.");
				return false;
			}
		}

		/// <inheritdoc/>
		public unsafe bool Open_FileSaveDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath)
		{
			filePath = string.Empty;

			try
			{
				using ComPtr<IFileSaveDialog> pDialog = default;
				HRESULT hr = pDialog.CoCreateInstance(CLSID.CLSID_FileSaveDialog, null, CLSCTX.CLSCTX_INPROC_SERVER);
				
				// Handle COM creation failure gracefully
				if (hr.Failed)
				{
					App.Logger.LogError("Failed to create IFileSaveDialog COM object. HRESULT: 0x{0:X8}", hr.Value);
					return false;
				}

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
					pDialog.Get()->SetFileTypes(extensions.ToArray());
				}

				// Get the default shell folder (My Computer)
				using ComPtr<IShellItem> pDefaultFolderShellItem = default;
				fixed (char* pszDefaultFolderPath = Environment.GetFolderPath(defaultFolder))
				{
					hr = PInvoke.SHCreateItemFromParsingName(
						pszDefaultFolderPath,
						null,
						IID.IID_IShellItem,
						(void**)pDefaultFolderShellItem.GetAddressOf());
					
					// Handle shell item creation failure gracefully
					if (hr.Failed)
					{
						App.Logger.LogWarning("Failed to create shell item for default folder '{0}'. HRESULT: 0x{1:X8}. Dialog will open without default folder.", Environment.GetFolderPath(defaultFolder), hr.Value);
						// Continue without setting default folder rather than failing completely
					}
				}

				// Folder picker
				if (pickFoldersOnly)
					pDialog.Get()->SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

				// Set the default folder to open in the dialog (only if creation succeeded)
				if (pDefaultFolderShellItem.Get() is not null)
				{
					pDialog.Get()->SetFolder(pDefaultFolderShellItem.Get());
					pDialog.Get()->SetDefaultFolder(pDefaultFolderShellItem.Get());
				}

				// Show the dialog
				hr = pDialog.Get()->Show(new HWND(hWnd));
				if (hr.Value == unchecked((int)0x800704C7)) // HRESULT_FROM_WIN32(ERROR_CANCELLED)
					return false;

				// Handle dialog show failure gracefully
				if (hr.Failed)
				{
					App.Logger.LogError("Failed to show FileSaveDialog. HRESULT: 0x{0:X8}", hr.Value);
					return false;
				}

				// Get the file that user chose
				using ComPtr<IShellItem> pResultShellItem = default;
				pDialog.Get()->GetResult(pResultShellItem.GetAddressOf());
				if (pResultShellItem.Get() is null)
					throw new COMException("FileSaveDialog returned invalid shell item.");
				pResultShellItem.Get()->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
				filePath = lpFilePath.ToString();

				return true;
			}
			catch (COMException comEx)
			{
				App.Logger.LogError(comEx, "COM failure while opening FileSaveDialog. HRESULT: 0x{0:X8}", comEx.HResult);
				return false;
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, "Unexpected error while opening FileSaveDialog.");
				return false;
			}
		}

		/// <inheritdoc/>
		public unsafe bool Open_NetworkConnectionDialog(nint hWnd, bool hideRestoreConnectionCheckBox = false, bool persistConnectionAtLogon = false, bool readOnlyPath = false, string? remoteNetworkName = null, bool useMostRecentPath = false)
		{
			if (useMostRecentPath && !string.IsNullOrEmpty(remoteNetworkName))
				throw new ArgumentException($"{nameof(useMostRecentPath)} cannot be set to true if {nameof(remoteNetworkName)} has a value.");

			NETRESOURCEW netResource = default;
			CONNECTDLGSTRUCTW connectDlgOptions = default;
			WIN32_ERROR res = default;

			if (hideRestoreConnectionCheckBox)
				connectDlgOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_HIDE_BOX;
			if (persistConnectionAtLogon)
				connectDlgOptions.dwFlags |= (CONNECTDLGSTRUCT_FLAGS.CONNDLG_PERSIST & CONNECTDLGSTRUCT_FLAGS.CONNDLG_NOT_PERSIST);
			if (useMostRecentPath)
				connectDlgOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_USE_MRU;
			if (readOnlyPath && !string.IsNullOrEmpty(remoteNetworkName))
				connectDlgOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_RO_PATH;

			fixed (char* pszRemoteName = remoteNetworkName)
			{
				netResource.dwType = NET_RESOURCE_TYPE.RESOURCETYPE_DISK;
				netResource.lpRemoteName = pszRemoteName;

				connectDlgOptions.cbStructure = (uint)sizeof(CONNECTDLGSTRUCTW);
				connectDlgOptions.hwndOwner = new(hWnd);
				connectDlgOptions.lpConnRes = &netResource;

				res = PInvoke.WNetConnectionDialog1W(ref connectDlgOptions);
			}

			// User canceled
			if ((uint)res == unchecked((uint)-1))
				return false;

			// Unexpected error happened
			if (res is not WIN32_ERROR.NO_ERROR)
				throw new Win32Exception("Failed to process the network connection dialog successfully.");

			return true;
		}
	}
}
