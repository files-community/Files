// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Win32PInvoke = Files.App.Helpers.NativeFileOperationsHelper;

namespace Files.App.Utils.Serialization
{
	internal sealed class DefaultSettingsSerializer : ISettingsSerializer
	{
		private string? _filePath;

		public bool CreateFile(string path)
		{
			var parentDir = SystemIO.Path.GetDirectoryName(path);
			if (string.IsNullOrEmpty(parentDir))
				return false;

			Win32PInvoke.CreateDirectoryFromApp(parentDir, IntPtr.Zero);

			var hFile = Win32PInvoke.CreateFileFromApp(
				path,
				Win32PInvoke.GENERIC_READ,
				Win32PInvoke.FILE_SHARE_READ,
				IntPtr.Zero,
				Win32PInvoke.OPEN_ALWAYS,
				(uint)Win32PInvoke.File_Attributes.BackupSemantics,
				IntPtr.Zero);

			if (hFile.IsHandleInvalid())
				return false;

			Win32PInvoke.CloseHandle(hFile);

			_filePath = path;
			return true;
		}

		public string ReadFromFile()
		{
			if (string.IsNullOrEmpty(_filePath))
				throw new ArgumentNullException(nameof(_filePath));

			return Win32PInvoke.ReadStringFromFile(_filePath);
		}

		public bool WriteToFile(string? text)
		{
			if (string.IsNullOrEmpty(_filePath))
				throw new ArgumentNullException(nameof(_filePath));

			return Win32PInvoke.WriteStringToFile(_filePath, text);
		}
	}
}
