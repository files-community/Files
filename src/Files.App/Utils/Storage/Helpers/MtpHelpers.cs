// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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
		public static string? ResolveMtpShellPath(string mtpPath)
		{
			var withoutPrefix = mtpPath.AsSpan(4);
			var sep = withoutPrefix.IndexOf('\\');
			var deviceName = (sep >= 0 ? withoutPrefix[..sep] : withoutPrefix).ToString();

			if (!_deviceParsingNames.TryGetValue(deviceName, out var parsingName))
			{
				unsafe
				{
					_deviceParsingNames[deviceName] = parsingName = FindDeviceParsingName(deviceName);
				}
			}

			return parsingName is null ? null
				: sep >= 0 ? Path.Combine(parsingName, withoutPrefix[(sep + 1)..].ToString())
				: parsingName;
		}

		private unsafe static string? FindDeviceParsingName(string deviceName)
		{
			HRESULT hr = PInvoke.SHGetKnownFolderItem(FOLDERID.FOLDERID_ComputerFolder, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, out IShellItem computerFolderItem);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return null;

			hr = computerFolderItem.BindToHandler(null, PInvoke.BHID_EnumItems, out IEnumShellItems? pEnum);
			if (hr.ThrowIfFailedOnDebug().Failed || pEnum is null)
				return null;

			IShellItem[] children = new IShellItem[1];
			while (true)
			{
				if (pEnum.Next(children) != HRESULT.S_OK)
					break;

				IShellItem pChild = children[0];

				pChild.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY, out var szName);
				var name = szName.ToString();
				PInvoke.CoTaskMemFree(szName.Value);

				if (!deviceName.StartsWith(name, StringComparison.OrdinalIgnoreCase))
					continue;

				pChild.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, out var szParsing);
				var result = szParsing.ToString();
				PInvoke.CoTaskMemFree(szParsing.Value);
				return result;
			}

			return null;
		}
	}
}
