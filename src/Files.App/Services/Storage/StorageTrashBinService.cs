// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
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
			var sid = WindowsIdentity.GetCurrent().User?.Value;
			if (string.IsNullOrEmpty(sid))
				return false;

			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				if (!drive.IsReady || drive.DriveType == System.IO.DriveType.Network)
					continue;

				string recyclePath = Path.Combine(drive.RootDirectory.FullName, "$RECYCLE.BIN", sid);
				if (!Directory.Exists(recyclePath))
					continue;

				try
				{
					var files = Directory.EnumerateFiles(recyclePath, "$I*", SearchOption.TopDirectoryOnly);
					if (files.Any())
						return true;
				}
				catch (UnauthorizedAccessException)
				{
				}
				catch (IOException)
				{
				}
			}

			return false;
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
			HRESULT hr = PInvoke.SHGetKnownFolderItem(FOLDERID.FOLDERID_RecycleBinFolder, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, out IShellItem pRecycleBinFolderShellItem);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return false;

			// Get IEnumShellItems for Recycle Bin folder
			hr = pRecycleBinFolderShellItem.BindToHandler(null, PInvoke.BHID_EnumItems, out IEnumShellItems? pEnumShellItems);
			if (hr.ThrowIfFailedOnDebug().Failed || pEnumShellItems is null)
				return false;

			// Initialize how to perform the operation
			hr = PInvoke.CoCreateInstance(typeof(FileOperation).GUID, null, CLSCTX.CLSCTX_LOCAL_SERVER, out IFileOperation? pFileOperation);
			if (hr.ThrowIfFailedOnDebug().Failed || pFileOperation is null)
				return false;

			hr = pFileOperation.SetOperationFlags(FILEOPERATION_FLAGS.FOF_NO_UI);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return false;

			hr = pFileOperation.SetOwnerWindow(new(MainWindow.Instance.WindowHandle));
			if (hr.ThrowIfFailedOnDebug().Failed)
				return false;

			IShellItem[] shellItems = new IShellItem[1];
			while (pEnumShellItems.Next(1, shellItems, null) == HRESULT.S_OK)
			{
				IShellItem shellItem = shellItems[0];

				if (shellItem is not IShellItem2 shellItem2)
					continue;

				hr = PInvoke.PSGetPropertyKeyFromName("System.Recycle.DeletedFrom", out var originalPathPropertyKey);
				if (hr.Failed)
					continue;

				hr = shellItem2.GetString(originalPathPropertyKey, out var szOriginalPath);
				if (hr.Failed)
					continue;

				try
				{
					// Get IShellItem of the original path
					hr = PInvoke.SHCreateItemFromParsingName(szOriginalPath.ToString(), null, out IShellItem originalPathShellItem);
					if (hr.Failed)
						continue;

					// Define the shell item to restore
					hr = pFileOperation.MoveItem(shellItem, originalPathShellItem, null!, null!);
				}
				finally
				{
					PInvoke.CoTaskMemFree(szOriginalPath);
				}
			}

			// Perform
			hr = pFileOperation.PerformOperations();
			if (hr.ThrowIfFailedOnDebug().Failed)
				return false;

			// Reset the icon
			PInvoke.SHUpdateRecycleBinIcon();

			return true;
		}
	}
}
