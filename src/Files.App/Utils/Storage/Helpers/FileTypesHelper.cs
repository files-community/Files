// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Collections.Concurrent;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Shell;

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Resolves the localized shell type name for a file extension (for example ".txt" produces "Text Document").
	/// </summary>
	public static class FileTypesHelper
	{
		// The type name is identical for every file of an extension, so cache it by extension.
		private static readonly ConcurrentDictionary<string, string> typeNameCache = new(StringComparer.OrdinalIgnoreCase);

		public static unsafe string GetLocalizedTypeName(string? extension)
		{
			if (string.IsNullOrEmpty(extension))
				return string.Empty;

			if (typeNameCache.TryGetValue(extension, out var cached))
				return cached;

			var typeName = string.Empty;
			SHFILEINFOW shfi = default;

			fixed (char* pExtension = extension)
			{
				// SHGFI_USEFILEATTRIBUTES resolves from the extension alone, so this never touches disk
				var result = PInvoke.SHGetFileInfo(
					pExtension,
					FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
					&shfi,
					(uint)sizeof(SHFILEINFOW),
					SHGFI_FLAGS.SHGFI_TYPENAME | SHGFI_FLAGS.SHGFI_USEFILEATTRIBUTES);

				if (result != 0 && shfi.szTypeName.Value[0] != '\0')
					typeName = shfi.szTypeName.ToString();
			}

			typeNameCache[extension] = typeName;
			return typeName;
		}
	}
}
