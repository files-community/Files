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
			return await STATask.Run(() =>
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
			}, App.Logger);
		}

		private unsafe bool RestoreAllTrashesInternal()
		{
			// Get IShellItem for Recycle Bin folder
			HRESULT hr = PInvoke.SHGetKnownFolderItem(FOLDERID.FOLDERID_RecycleBinFolder, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, hToken: HANDLE.Null, IID.IID_IShellItem, out var recycleBinFolderObj);
			var recycleBinFolder = (IShellItem)recycleBinFolderObj;

			// Get IEnumShellItems for Recycle Bin folder
			hr = recycleBinFolder.BindToHandler(null, BHID.BHID_EnumItems, IID.IID_IEnumShellItems, out var enumShellItemsObj);
			var enumShellItems = (IEnumShellItems)enumShellItemsObj;

			// Initialize how to perform the operation
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_FileOperation, null, CLSCTX.CLSCTX_LOCAL_SERVER, IID.IID_IFileOperation, out var fileOperationObj);
			var fileOperation = (IFileOperation)fileOperationObj;
			hr = fileOperation.SetOperationFlags(FILEOPERATION_FLAGS.FOF_NO_UI);
			hr = fileOperation.SetOwnerWindow(new(MainWindow.Instance.WindowHandle));

			var childItemArray = new IShellItem[1];
			while (enumShellItems.Next(1, childItemArray) == HRESULT.S_OK)
			{
				IShellItem childItem = childItemArray[0];

				// Get the original path
				IShellItem2 childItem2 = (IShellItem2)childItem;
				hr = PInvoke.PSGetPropertyKeyFromName("System.Recycle.DeletedFrom", out var originalPathPropertyKey);
				hr = childItem2.GetString(originalPathPropertyKey, out var originalPath);

				// Get IShellItem of the original path
				hr = PInvoke.SHCreateItemFromParsingName(originalPath.ToString(), null, typeof(IShellItem).GUID, out var originalPathItemObj);
				var originalPathItem = (IShellItem)originalPathItemObj;

				// Define the shell item to restore
				hr = fileOperation.MoveItem(childItem, originalPathItem, default, null);
			}

			// Perform
			hr = fileOperation.PerformOperations();

			// Reset the icon
			PInvoke.SHUpdateRecycleBinIcon();

			return true;
		}
	}
}
