// Copyright (c) Files Community
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
		public unsafe bool Open_FileOpenDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath, Guid? clientGuid = null)
		{
			filePath = string.Empty;

			try
			{
				HRESULT hr = PInvoke.CoCreateInstance(typeof(FileOpenDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out IFileOpenDialog? pDialog);
				
				// Handle COM creation failure gracefully
				if (hr.Failed || pDialog is null)
				{
					App.Logger.LogError("Failed to create IFileOpenDialog COM object. HRESULT: 0x{0:X8}", hr.Value);
					return false;
				}

				SetFileTypes(pDialog, filters);

				// Get the default shell folder (My Computer)
				IShellItem? pDefaultFolderShellItem = null;
				hr = PInvoke.SHCreateItemFromParsingName(Environment.GetFolderPath(defaultFolder), null, out IShellItem defaultFolderShellItem);

				// Handle shell item creation failure gracefully
				if (hr.Failed)
				{
					App.Logger.LogWarning("Failed to create shell item for default folder '{0}'. HRESULT: 0x{1:X8}. Dialog will open without default folder.", defaultFolder, hr.Value);
					// Continue without setting default folder rather than failing completely
				}
				else
				{
					pDefaultFolderShellItem = defaultFolderShellItem;
				}

				// Folder picker
				if (pickFoldersOnly)
					pDialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

				// Persist dialog state (including the last browsed folder) under the caller's GUID
				if (clientGuid is { } guid)
					pDialog.SetClientGuid(in guid);

				// Set the default folder to open in the dialog (only if creation succeeded)
				if (pDefaultFolderShellItem is not null)
				{
					// SetFolder forces the dialog to always open at this folder, which would override the persisted state
					if (clientGuid is null)
						pDialog.SetFolder(pDefaultFolderShellItem);

					pDialog.SetDefaultFolder(pDefaultFolderShellItem);
				}

				// Show the dialog
				hr = pDialog.Show(new HWND(hWnd));
				if (hr.Value == unchecked((int)0x800704C7)) // HRESULT_FROM_WIN32(ERROR_CANCELLED)
					return false;

				// Handle dialog show failure gracefully
				if (hr.Failed)
				{
					App.Logger.LogError("Failed to show FileSaveDialog. HRESULT: 0x{0:X8}", hr.Value);
					return false;
				}

				// Get the file that user chose
				pDialog.GetResult(out IShellItem pResultShellItem);
				if (pResultShellItem is null)
					throw new COMException("FileOpenDialog returned invalid shell item.");
				pResultShellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
				try
				{
					filePath = lpFilePath.ToString();
				}
				finally
				{
					PInvoke.CoTaskMemFree(lpFilePath);
				}

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
				HRESULT hr = PInvoke.CoCreateInstance(typeof(FileSaveDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out IFileSaveDialog? pDialog);
				
				// Handle COM creation failure gracefully
				if (hr.Failed || pDialog is null)
				{
					App.Logger.LogError("Failed to create IFileSaveDialog COM object. HRESULT: 0x{0:X8}", hr.Value);
					return false;
				}

				SetFileTypes(pDialog, filters);

				// Get the default shell folder (My Computer)
				IShellItem? pDefaultFolderShellItem = null;
				hr = PInvoke.SHCreateItemFromParsingName(Environment.GetFolderPath(defaultFolder), null, out IShellItem defaultFolderShellItem);
					
				// Handle shell item creation failure gracefully
				if (hr.Failed)
				{
					App.Logger.LogWarning("Failed to create shell item for default folder '{0}'. HRESULT: 0x{1:X8}. Dialog will open without default folder.", defaultFolder, hr.Value);
					// Continue without setting default folder rather than failing completely
				}
				else
				{
					pDefaultFolderShellItem = defaultFolderShellItem;
				}

				// Folder picker
				if (pickFoldersOnly)
					pDialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

				// Set the default folder to open in the dialog (only if creation succeeded)
				if (pDefaultFolderShellItem is not null)
				{
					pDialog.SetFolder(pDefaultFolderShellItem);
					pDialog.SetDefaultFolder(pDefaultFolderShellItem);
				}

				// Show the dialog
				hr = pDialog.Show(new HWND(hWnd));
				if (hr.Value == unchecked((int)0x800704C7)) // HRESULT_FROM_WIN32(ERROR_CANCELLED)
					return false;

				// Handle dialog show failure gracefully
				if (hr.Failed)
				{
					App.Logger.LogError("Failed to show FileSaveDialog. HRESULT: 0x{0:X8}", hr.Value);
					return false;
				}

				// Get the file that user chose
				pDialog.GetResult(out IShellItem pResultShellItem);
				if (pResultShellItem is null)
					throw new COMException("FileSaveDialog returned invalid shell item.");
				pResultShellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
				try
				{
					filePath = lpFilePath.ToString();
				}
				finally
				{
					PInvoke.CoTaskMemFree(lpFilePath);
				}

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

		private static unsafe void SetFileTypes(IFileDialog dialog, string[] filters)
		{
			if (filters.Length == 0 || filters.Length % 2 != 0)
				return;

			var filterSpecs = new COMDLG_FILTERSPEC[filters.Length / 2];
			var allocations = new nint[filters.Length];
			try
			{
				for (var filterIndex = 0; filterIndex < filterSpecs.Length; filterIndex++)
				{
					var sourceIndex = filterIndex * 2;
					allocations[sourceIndex] = Marshal.StringToHGlobalUni(filters[sourceIndex]);
					allocations[sourceIndex + 1] = Marshal.StringToHGlobalUni(filters[sourceIndex + 1]);
					filterSpecs[filterIndex] = new COMDLG_FILTERSPEC
					{
						pszName = (char*)allocations[sourceIndex],
						pszSpec = (char*)allocations[sourceIndex + 1],
					};
				}

				dialog.SetFileTypes(filterSpecs);
			}
			finally
			{
				foreach (var allocation in allocations)
					Marshal.FreeHGlobal(allocation);
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
				connectDlgOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_PERSIST;
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
