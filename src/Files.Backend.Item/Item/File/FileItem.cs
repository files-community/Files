using System;
using System.ComponentModel;

namespace Files.Backend.Item
{
    internal class FileItem : IFileItem
    {
        public event PropertyChangedEventHandler PropertyChanged
        {
            add {}
            remove {}
        }

        public string Path { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;

        public FileAttributes FileAttribute { get; init; } = FileAttributes.None;

        public ByteSize Size { get; init; } = ByteSize.Zero;

        public DateTime DateCreated { get; init; }
        public DateTime DateModified { get; init; }
        public DateTime DateAccessed { get; init; }
    }
}
