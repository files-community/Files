using System;
using System.IO;
using Files.Shared.Extensions;

using static Files.Uwp.Helpers.NativeFileOperationsHelper;

#nullable enable

namespace Files.Uwp.Serialization.Implementation
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

        public string? ReadFromFile()
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
