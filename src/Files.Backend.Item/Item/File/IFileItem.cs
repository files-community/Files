using System;

namespace Files.Backend.Item
{
    public interface IFileItem : IItem
    {
        FileAttributes FileAttribute { get; }

        ByteSize Size { get; }

        DateTime DateCreated { get; }
        DateTime DateModified { get; }
        DateTime DateAccessed { get; }
    }
}
