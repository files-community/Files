// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Utils.Storage
{
	public static class MtpHelpers
	{
		private static readonly ConcurrentDictionary<string, string?> _deviceParsingNames = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Resolves a <c>\\?\DeviceName\path</c> to a shell Portable Devices namespace path
		/// so that shell APIs like <see cref="IShellItemImageFactory"/> work correctly.
		/// </summary>
		public unsafe static string? ResolveMtpShellPath(string mtpPath)
		{
			var withoutPrefix = mtpPath.AsSpan(4);
			var sep = withoutPrefix.IndexOf('\\');
			var deviceName = (sep >= 0 ? withoutPrefix[..sep] : withoutPrefix).ToString();

			if (!_deviceParsingNames.TryGetValue(deviceName, out var parsingName))
				_deviceParsingNames[deviceName] = parsingName = FindDeviceParsingName(deviceName);

			return parsingName is null ? null
				: sep >= 0 ? Path.Combine(parsingName, withoutPrefix[(sep + 1)..].ToString())
				: parsingName;
		}

		private unsafe static string? FindDeviceParsingName(string deviceName)
		{
			using ComPtr<IShellItem> pComputer = default;
			Guid folderId = new("0AC0837C-BBF8-452A-850D-79D08E667CA7");
			PInvoke.SHGetKnownFolderItem(&folderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, HANDLE.Null, IID.IID_IShellItem, (void**)pComputer.GetAddressOf());

			if (pComputer.IsNull)
				return null;

			using ComPtr<IEnumShellItems> pEnum = default;
			pComputer.Get()->BindToHandler(null, BHID.BHID_EnumItems, IID.IID_IEnumShellItems, (void**)pEnum.GetAddressOf());

			if (pEnum.IsNull)
				return null;

			while (true)
			{
				using ComPtr<IShellItem> pChild = default;
				if (pEnum.Get()->Next(1, pChild.GetAddressOf()) != HRESULT.S_OK)
					break;

				pChild.Get()->GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY, out var szName);
				var name = szName.ToString();
				PInvoke.CoTaskMemFree(szName.Value);

				if (!deviceName.StartsWith(name, StringComparison.OrdinalIgnoreCase))
					continue;

				pChild.Get()->GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, out var szParsing);
				var result = szParsing.ToString();
				PInvoke.CoTaskMemFree(szParsing.Value);
				return result;
			}

			return null;
		}
	}
}
