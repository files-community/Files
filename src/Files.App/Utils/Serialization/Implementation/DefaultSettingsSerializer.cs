// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using static Files.App.Helpers.Win32Helper;

namespace Files.App.Utils.Serialization.Implementation
{
	internal sealed class DefaultSettingsSerializer : ISettingsSerializer
	{
		private string? _filePath;

		public bool CreateFile(string path)
		{
			PInvoke.CreateDirectory(Path.GetDirectoryName(path), null);

			using var hFile = PInvoke.CreateFile(path, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ, FILE_SHARE_MODE.FILE_SHARE_READ, null, FILE_CREATION_DISPOSITION.OPEN_ALWAYS, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS, null);
			if (hFile.IsInvalid)
			{
				return false;
			}

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
			ArgumentNullException.ThrowIfNull(_filePath);

			return ReadStringFromFile(_filePath) ?? string.Empty;
		}

		public bool WriteToFile(string text)
		{
			ArgumentNullException.ThrowIfNull(_filePath);

			return WriteStringToFile(_filePath, text);
		}
	}
}
