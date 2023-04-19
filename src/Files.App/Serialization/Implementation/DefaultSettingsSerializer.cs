using Files.Shared.Extensions;
using static Files.App.Helpers.NativeFileOperationsHelper;

namespace Files.App.Serialization.Implementation
{
	internal sealed class DefaultSettingsSerializer : ISettingsSerializer
	{
		private string? _filePath;

		public bool CreateFile(string path)
		{
			CreateDirectoryFromApp(Path.GetDirectoryName(path), IntPtr.Zero);

			var hFile = CreateFileFromApp(path, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_ALWAYS, (uint)File_Attributes.BackupSemantics, IntPtr.Zero);
			if (hFile.IsHandleInvalid())
			{
				return false;
			}

			CloseHandle(hFile);

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
