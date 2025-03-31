// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage.Storables
{
	public static partial class WindowsStorableHelpers
	{
		public unsafe static HRESULT GetPropertyValue<TValue>(this IWindowsStorable storable, string propKey, out TValue value)
		{
			using ComPtr<IShellItem2> pShellItem2 = default;
			var shellItem2Iid = typeof(IShellItem2).GUID;
			HRESULT hr = storable.ThisPtr.Get()->QueryInterface(&shellItem2Iid, (void**)pShellItem2.GetAddressOf());
			hr = PInvoke.PSGetPropertyKeyFromName(propKey, out var originalPathPropertyKey);
			hr = pShellItem2.Get()->GetString(originalPathPropertyKey, out var szOriginalPath);

			if (typeof(TValue) == typeof(string))
			{
				value = (TValue)(object)szOriginalPath.ToString();
				return hr;
			}
			else
			{
				value = default!;
				return HRESULT.E_FAIL;
			}
		}

		public unsafe static bool HasShellAttributes(this IWindowsStorable storable, SFGAO_FLAGS attributes)
		{
			return storable.ThisPtr.Get()->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded &&
				returnedAttributes == attributes;
		}

		public unsafe static bool HasShellAttributes(this ComPtr<IShellItem> pShellItem, SFGAO_FLAGS attributes)
		{
			return pShellItem.Get()->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded &&
				returnedAttributes == attributes;
		}

		public unsafe static string GetDisplayName(this IWindowsStorable storable, SIGDN options = SIGDN.SIGDN_FILESYSPATH)
		{
			using ComHeapPtr<PWSTR> pszName = default;
			HRESULT hr = storable.ThisPtr.Get()->GetDisplayName(options, (PWSTR*)pszName.GetAddressOf());

			return hr.ThrowIfFailedOnDebug().Succeeded
				? (*pszName.Get()).ToString()
				: string.Empty;
		}

		public static bool TryGetFileAttributes(this IWindowsStorable storable, out FILE_FLAGS_AND_ATTRIBUTES attributes)
		{
			attributes = (FILE_FLAGS_AND_ATTRIBUTES)PInvoke.GetFileAttributes(storable.GetDisplayName());

			if ((uint)attributes is PInvoke.INVALID_FILE_ATTRIBUTES)
			{
				attributes = 0;
				return false;
			}
			else
			{
				return true;
			}
		}

		public static bool TrySetFileAttributes(this IWindowsStorable storable, FILE_FLAGS_AND_ATTRIBUTES attributes)
		{
			if (attributes is FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_COMPRESSED)
				return storable.TryToggleFileCompressedAttribute(true);

			if (!storable.TryGetFileAttributes(out var previousAttributes))
				return false;
			return PInvoke.SetFileAttributes(storable.GetDisplayName(), previousAttributes | attributes);
		}

		public static bool TryUnsetFileAttributes(this IWindowsStorable storable, FILE_FLAGS_AND_ATTRIBUTES attributes)
		{
			if (attributes is FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_COMPRESSED)
				return storable.TryToggleFileCompressedAttribute(false);

			if (!storable.TryGetFileAttributes(out var previousAttributes))
				return false;
			return PInvoke.SetFileAttributes(storable.GetDisplayName(), previousAttributes & ~attributes);
		}

		public unsafe static bool TryToggleFileCompressedAttribute(this IWindowsStorable storable, bool value)
		{
			// GENERIC_READ | GENERIC_WRITE flags are needed here
			// FILE_FLAG_BACKUP_SEMANTICS is used to open directories
			using var hFile = PInvoke.CreateFile(
				storable.GetDisplayName(),
				(uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE | FILE_ACCESS_RIGHTS.FILE_WRITE_ATTRIBUTES),
				FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
				lpSecurityAttributes: null,
				FILE_CREATION_DISPOSITION.OPEN_EXISTING,
				FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS,
				hTemplateFile: null);

			if (hFile.IsInvalid)
				return false;

			var bytesReturned = 0u;
			var compressionFormat = value
				? COMPRESSION_FORMAT.COMPRESSION_FORMAT_DEFAULT
				: COMPRESSION_FORMAT.COMPRESSION_FORMAT_NONE;

			var result = PInvoke.DeviceIoControl(
				new(hFile.DangerousGetHandle()),
				PInvoke.FSCTL_SET_COMPRESSION,
				&compressionFormat,
				sizeof(ushort),
				null,
				0u,
				&bytesReturned);

			return result;
		}
	}
}
