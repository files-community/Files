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
			// Get all items in the Recycle Bin

			fixed (char* cRecycleBinFolderPath = new char[256])
			{
				PWSTR szRecycleBinFolderPath = new(cRecycleBinFolderPath);
				PInvoke.SHGetFolderPath(HWND.Null, (int)0x000a /*CSIDL_BITBUCKET*/, HANDLE.Null, (uint)SHGFP_TYPE.SHGFP_TYPE_CURRENT, szRecycleBinFolderPath);

				PInvoke.SHCreateItemFromParsingName(szRecycleBinFolderPath.ToString(), null, typeof(IShellItem).GUID, out var pRecycleBinShellItem);

				IShellItem recycleBinShellItem = (IShellItem)pRecycleBinShellItem;

				Guid bindIdEnumItemsGuid = new(0x94f60519, 0x2850, 0x4924, 0xaa, 0x5a, 0xd1, 0x5e, 0x84, 0x86, 0x80, 0x39);

				recycleBinShellItem.BindToHandler(
					null,
					bindIdEnumItemsGuid,
					typeof(IEnumShellItems).GUID,
					out var pRecycleBinFolderEnumerable);

				IEnumShellItems recycleBinFolderEnumerable = (IEnumShellItems)pRecycleBinFolderEnumerable;

				while (true)
				{
					//IShellItem spsi;
					//recycleBinFolderEnumerable.Next(1, [spsi], null);
					//if (spsi is null)
					//	break;

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
				}
			}

			// Reset the icon.
			Win32PInvoke.SHUpdateRecycleBinIcon();

			return true;
		}
	}
}
