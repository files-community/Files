// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Extensions;
using System;
using System.IO;
using static Files.App.Helpers.Win32Helper;

namespace Files.App.Utils.Serialization.Implementation
{
	internal sealed class DefaultSettingsSerializer : ISettingsSerializer
	{
		private string? _filePath;

		public bool CreateFile(string path)
		{
			Win32PInvoke.CreateDirectoryFromApp(Path.GetDirectoryName(path), IntPtr.Zero);

			var hFile = Win32PInvoke.CreateFileFromApp(path, Win32PInvoke.GENERIC_READ, Win32PInvoke.FILE_SHARE_READ, IntPtr.Zero, Win32PInvoke.OPEN_ALWAYS, (uint)Win32PInvoke.File_Attributes.BackupSemantics, IntPtr.Zero);
			if (hFile.IsHandleInvalid())
			{
				return false;
			}

			Win32PInvoke.CloseHandle(hFile);

			_filePath = path;
			return true;
		}

		/// <summary>
		/// Reads a file to a string
		/// </summary>
		/// <returns>A string value or string.Empty if nothing is present in the file</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public string ReadFromFile()
		{
			_ = _filePath ?? throw new ArgumentNullException(nameof(_filePath));

			return ReadStringFromFile(_filePath);
		}

		public bool WriteToFile(string? text)
		{
			_ = _filePath ?? throw new ArgumentNullException(nameof(_filePath));

			return WriteStringToFile(_filePath, text);
		}
	}
}
