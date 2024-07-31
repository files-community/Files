// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using static Vanara.PInvoke.User32;

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsRecycleBinService"/>
	public class WindowsRecycleBinService : IWindowsRecycleBinService
	{
		/// <inheritdoc/>
		public RecycleBinWatcher Watcher { get; private set; } = new();

		/// <inheritdoc/>
		public async Task<List<ShellFileItem>> GetAllRecycleBinFoldersAsync()
		{
			return (await Win32Helper.GetShellFolderAsync(Constants.UserEnvironmentPaths.RecycleBinPath, false, true, 0, int.MaxValue)).Enumerate;
		}

		/// <inheritdoc/>
		public ulong GetSize()
		{
			return (ulong)Win32Helper.QueryRecycleBin().BinSize;
		}

		/// <inheritdoc/>
		public bool HasItems()
		{
			return Win32Helper.QueryRecycleBin().NumItems > 0;
		}

		/// <inheritdoc/>
		public bool IsRecycled(string? path)
		{
			return
				!string.IsNullOrWhiteSpace(path) &&
				RegexHelpers.RecycleBinPath().IsMatch(path);
		}

		/// <inheritdoc/>
		public async Task<bool> IsRecyclableAsync(string? path)
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
		public bool DeleteAllAsync()
		{
			var fRes = PInvoke.SHEmptyRecycleBin(
				new(),
				string.Empty,
				0x00000001 | 0x00000002 /* SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI */)
			.Succeeded;

			return fRes;
		}

		/// <inheritdoc/>
		public unsafe bool RestoreAllAsync()
		{
			var recycleBinFolderKnownFolderId = new Guid(0xB7534046, 0x3ECB, 0x4C18, 0xBE, 0x4E, 0x64, 0xCD, 0x4C, 0xB7, 0xD6, 0xAC);
			var shellItemGuid = typeof(IShellItem).GUID;

			PInvoke.SHGetKnownFolderItem(
				&recycleBinFolderKnownFolderId,
				KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT,
				HANDLE.Null,
				&shellItemGuid,
				out var recycleBinObject);

			IShellItem recycleBin = (IShellItem)recycleBinObject;

			Guid iid = typeof(IEnumShellItems).GUID;
			recycleBin.BindToHandler(null, iid, iid, out object itemsObject);
			IEnumShellItems items = (IEnumShellItems)itemsObject;

			//IShellItem item;
			//while (items.Next(1, [item]) == HRESULT.S_OK)
			//{
			//	fixed (char* pszPath = item.Path + '\0')
			//	{
			//		PCZZWSTR pwszPath = new PCZZWSTR(pszPath);

			//		var fileOperationData = new SHFILEOPSTRUCTW
			//		{
			//			wFunc = 0x0003, /* FO_DELETE */
			//			pFrom = pwszPath,
			//			fFlags = 0x0004 | 0x0010 | 0x0400 | 0x0200, /* FOF_NO_UI == FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_NOCONFIRMMKDIR */
			//		};

			//		PInvoke.SHFileOperation(ref fileOperationData);
			//	}
			//}

			// Reset the icon.
			Win32PInvoke.SHUpdateRecycleBinIcon();

			return true;
		}
	}
}
