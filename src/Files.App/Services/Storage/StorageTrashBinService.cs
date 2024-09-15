// Copyright (c) 2018-2024 Files Community
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
			var fRes = PInvoke.SHEmptyRecycleBin(
				new(),
				string.Empty,
				0x00000001 | 0x00000002 /* SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI */)
			.Succeeded;

			return fRes;
		}

		/// <inheritdoc/>
		public unsafe bool RestoreAllTrashes()
		{
			IShellItem* recycleBinFolderShellItem = default;
			IEnumShellItems* enumShellItems = default;
			IFileOperation* pFileOperation = default;
			IShellItem* pShellItem = default;

			try
			{
				// Get IShellItem for Recycle Bin
				var recycleBinFolderId = FOLDERID.FOLDERID_RecycleBinFolder;
				var shellItemGuid = typeof(IShellItem).GUID;
				PInvoke.SHGetKnownFolderItem(&recycleBinFolderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, HANDLE.Null, &shellItemGuid, (void**)&recycleBinFolderShellItem);

				// Get IEnumShellItems for Recycle Bin
				Guid enumShellItemGuid = typeof(IEnumShellItems).GUID;
				var enumItemsBHID = BHID.BHID_EnumItems;
				recycleBinFolderShellItem->BindToHandler(null, &enumItemsBHID, &enumShellItemGuid, (void**)&enumShellItems);

				// Initialize how to perform the operation
				PInvoke.CoCreateInstance(typeof(FileOperation).GUID, null, CLSCTX.CLSCTX_LOCAL_SERVER, out pFileOperation);
				pFileOperation->SetOperationFlags(FILEOPERATION_FLAGS.FOF_NO_UI);
				pFileOperation->SetOwnerWindow(new(MainWindow.Instance.WindowHandle));

				while (enumShellItems->Next(1, &pShellItem) == HRESULT.S_OK)
				{
					// Get original path
					pShellItem->QueryInterface(typeof(IShellItem2).GUID, out var pShellItem2Ptr);
					var pShellItem2 = (IShellItem2*)pShellItem2Ptr;
					PInvoke.PSGetPropertyKeyFromName("System.Recycle.DeletedFrom", out var originalPathPropertyKey);
					pShellItem2->GetString(originalPathPropertyKey, out var szOriginalPath);
					pShellItem2->Release();

					// Get IShellItem of the original path
					PInvoke.SHCreateItemFromParsingName(szOriginalPath.ToString(), null, typeof(IShellItem).GUID, out var pOriginalPathShellItemPtr);
					var pOriginalPathShellItem = (IShellItem*)pOriginalPathShellItemPtr;

					// Define to move the shell item
					pFileOperation->MoveItem(pShellItem, pOriginalPathShellItem, new PCWSTR(), null);
				}

				// Perform
				pFileOperation->PerformOperations();

				// Reset the icon
				Win32PInvoke.SHUpdateRecycleBinIcon();

				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				recycleBinFolderShellItem->Release();
				enumShellItems->Release();
				pFileOperation->Release();
				pShellItem->Release();
			}
		}
	}
}
