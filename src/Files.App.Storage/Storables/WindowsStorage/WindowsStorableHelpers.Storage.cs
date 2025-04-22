// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public unsafe static partial class WindowsStorableHelpers
	{
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

		public static bool TryToggleFileCompressedAttribute(this IWindowsStorable storable, bool value)
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

		public static bool TryShowFormatDriveDialog(HWND hWnd, uint driveLetterIndex, SHFMT_ID id, SHFMT_OPT options)
		{
			// NOTE: This calls an undocumented elevatable COM class, shell32.dll!CFormatEngine so this doesn't need to be elevated beforehand.
			var result = PInvoke.SHFormatDrive(hWnd, driveLetterIndex, id, options);
			return result is 0xFFFF;
		}
	}
}
