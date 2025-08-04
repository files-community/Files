// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Services
{
	/// <inheritdoc cref="IStorageTrashBinService"/>
	public class StorageTrashBinService : IStorageTrashBinService
	{
		/// <inheritdoc/>
		public RecycleBinWatcher Watcher { get; private set; } = new();

		/// <inheritdoc/>
		public async Task<List<ShellFileItem>> GetAllRecycleBinFoldersAsync()
		{
			return (await Win32Helper.GetShellFolderAsync(Constants.UserEnvironmentPaths.RecycleBinPath, false, true, 0, int.MaxValue)).Enumerate;
		}

		/// <inheritdoc/>
		public (bool HasRecycleBin, long NumItems, long BinSize) QueryRecycleBin(string drive = "")
		{
			SHQUERYRBINFO queryBinInfo = default;
			queryBinInfo.cbSize = (uint)Marshal.SizeOf(queryBinInfo);

			var hRes = PInvoke.SHQueryRecycleBin(drive, ref queryBinInfo);
			return hRes == HRESULT.S_OK
				? (true, queryBinInfo.i64NumItems, queryBinInfo.i64Size)
				: (false, 0, 0);
		}

		/// <inheritdoc/>
		public ulong GetSize()
		{
			return (ulong)QueryRecycleBin().BinSize;
		}

		/// <inheritdoc/>
		public bool HasItems()
		{
			return QueryRecycleBin().NumItems > 0;
		}

		/// <inheritdoc/>
		public bool IsUnderTrashBin(string? path)
		{
			return
				!string.IsNullOrWhiteSpace(path) &&
				RegexHelpers.RecycleBinPath().IsMatch(path);
		}

		/// <inheritdoc/>
		public async Task<bool> CanGoTrashBin(string? path)
		{
			if (string.IsNullOrEmpty(path) ||
				path.StartsWith(@"\\?\", StringComparison.Ordinal))
				return false;

			var result = await FileOperationsHelpers.TestRecycleAsync(path.Split('|'));

			return
				result.Item1 &= result.Item2 is not null &&
				result.Item2.Items.All(x => x.Succeeded);
		}

		/// <inheritdoc/>
		public bool EmptyTrashBin()
		{
			// TODO: Use IFileOperation instead of its wrapper for the operation status to be reported.
			var fRes = PInvoke.SHEmptyRecycleBin(
				new(),
				string.Empty,
				0x00000001 | 0x00000002 /* SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI */)
			.Succeeded;

			return fRes;
		}

		/// <inheritdoc/>
		public async Task<bool> RestoreAllTrashesAsync()
		{
			return await Win32Helper.StartSTATask(() =>
			{
				try
				{
					RestoreAllTrashesInternal();

					return true;
				}
				catch
				{
					return false;
				}
			});
		}

		private unsafe bool RestoreAllTrashesInternal()
		{
			// Get IShellItem for Recycle Bin folder
			using ComPtr<IShellItem> pRecycleBinFolderShellItem = default;
			HRESULT hr = PInvoke.SHGetKnownFolderItem(FOLDERID.FOLDERID_RecycleBinFolder, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, HANDLE.Null, IID.IID_IShellItem, (void**)pRecycleBinFolderShellItem.GetAddressOf());

			// Get IEnumShellItems for Recycle Bin folder
			using ComPtr<IEnumShellItems> pEnumShellItems = default;
			hr = pRecycleBinFolderShellItem.Get()->BindToHandler(null, BHID.BHID_EnumItems, IID.IID_IEnumShellItems, (void**)pEnumShellItems.GetAddressOf());

			// Initialize how to perform the operation
			using ComPtr<IFileOperation> pFileOperation = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_FileOperation, null, CLSCTX.CLSCTX_LOCAL_SERVER, IID.IID_IFileOperation, (void**)pFileOperation.GetAddressOf());
			hr = pFileOperation.Get()->SetOperationFlags(FILEOPERATION_FLAGS.FOF_NO_UI);
			hr = pFileOperation.Get()->SetOwnerWindow(new(MainWindow.Instance.WindowHandle));

			using ComPtr<IShellItem> pShellItem = default;
			while (pEnumShellItems.Get()->Next(1, pShellItem.GetAddressOf()) == HRESULT.S_OK)
			{
				// Get the original path
				using ComPtr<IShellItem2> pShellItem2 = default;
				hr = pShellItem.Get()->QueryInterface(IID.IID_IShellItem2, (void**)pShellItem2.GetAddressOf());
				hr = PInvoke.PSGetPropertyKeyFromName("System.Recycle.DeletedFrom", out var originalPathPropertyKey);
				hr = pShellItem2.Get()->GetString(originalPathPropertyKey, out var szOriginalPath);

				// Get IShellItem of the original path
				hr = PInvoke.SHCreateItemFromParsingName(szOriginalPath.ToString(), null, typeof(IShellItem).GUID, out var pOriginalPathShellItemPtr);
				var pOriginalPathShellItem = (IShellItem*)pOriginalPathShellItemPtr;

				// Define the shell item to restore
				hr = pFileOperation.Get()->MoveItem(pShellItem.Get(), pOriginalPathShellItem, default(PCWSTR), null);
			}

			// Perform
			hr = pFileOperation.Get()->PerformOperations();

			// Reset the icon
			PInvoke.SHUpdateRecycleBinIcon();

			return true;
		}
	}
}
