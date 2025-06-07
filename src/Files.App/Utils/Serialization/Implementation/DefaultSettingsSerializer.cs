// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.Utils.Serialization.Implementation
{
	internal sealed class DefaultSettingsSerializer : ISettingsSerializer
	{
		private string? _filePath;

		public unsafe bool CreateFile(string path)
		{
			PInvoke.CreateDirectoryFromApp(Path.GetDirectoryName(path), null);

			HANDLE hFile = default;
			fixed (char* pPath = path)
			{
				hFile = PInvoke.CreateFile(
					pPath,
					(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ,
					FILE_SHARE_MODE.FILE_SHARE_READ,
					(SECURITY_ATTRIBUTES*)null,
					FILE_CREATION_DISPOSITION.OPEN_ALWAYS,
					FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS,
					HANDLE.Null);
			}

			// File handle is invalid
			if (hFile.IsNull || hFile.Value == (void*)-1)
				return false;

			PInvoke.CloseHandle(hFile);

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

			return Win32Helper.ReadStringFromFile(_filePath);
		}

		public bool WriteToFile(string? text)
		{
			_ = _filePath ?? throw new ArgumentNullException(nameof(_filePath));

			return Win32Helper.WriteStringToFile(_filePath, text);
		}
	}
}
