using Files.Backend.Item.Tools;
using FluentFTP;
using System;
using System.ComponentModel;

namespace Files.Backend.Item
{
    internal class FtpItem : IFtpItem
    {
        public event PropertyChangedEventHandler PropertyChanged
        {
            add {}
            remove {}
        }

        public string Path { get; }
        public string Name { get; }

        public FileAttributes FileAttribute { get; }

        public ByteSize Size { get; }

        public DateTime DateCreated { get; }
        public DateTime DateModified { get; }
        public DateTime DateAccessed => DateModified;

        public FtpItem(FtpListItem item, string folder)
        {
            Path = folder.CombineNameToPath(item.Name);
            Name = item.Name;
            FileAttribute = item.Type == FtpFileSystemObjectType.File ? FileAttributes.None : FileAttributes.Directory;
            Size = item.Size < 0 ? 0L : item.Size;
            DateCreated = Clean(item.RawCreated);
            DateModified = Clean(item.RawModified);
        }

        private static DateTime Clean(DateTime date)
            => date < DateTime.FromFileTimeUtc(0) ? DateTime.MinValue : date;
    }
}
