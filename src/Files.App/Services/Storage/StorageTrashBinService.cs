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
			return await Win32Helper.StartSTATask(async () =>
			{
				try
				{
					HRESULT hr = default;
					IChildFolder recycleBinFolder = new WindowsFolder(PInvoke.FOLDERID_RecycleBinFolder);

					// Initialize how to perform the operation
					using var bulkOperations = new WindowsBulkOperations(new(MainWindow.Instance.WindowHandle), FILEOPERATION_FLAGS.FOF_NO_UI);

					await foreach (WindowsStorable item in recycleBinFolder.GetItemsAsync())
					{
						item.GetPropertyValue("System.Recycle.DeletedFrom", out string originalLocationFolderPath);

						if (WindowsStorable.TryParse(originalLocationFolderPath, out var originalLocationItem) &&
							originalLocationItem is WindowsFolder originalLocationFolder)
							hr = bulkOperations.QueueMoveOperation(item, originalLocationFolder, null);
					}

					// Perform
					hr = bulkOperations.PerformAllOperations();

					// Reset the icon
					Win32PInvoke.SHUpdateRecycleBinIcon();

					return true;
				}
				catch
				{
					return false;
				}
			});
		}
	}
}
