using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IO = System.IO;

namespace Files.Backend.Item
{
    internal static class FileAttributeConverter
    {
        private static readonly IImmutableDictionary<IO.FileAttributes, FileAttributes> attributes
            = new Dictionary<IO.FileAttributes, FileAttributes>
            {
                [IO.FileAttributes.Archive] = FileAttributes.Archive,
                [IO.FileAttributes.Compressed] = FileAttributes.Compressed,
                [IO.FileAttributes.Device] = FileAttributes.Device,
                [IO.FileAttributes.Directory] = FileAttributes.Directory,
                [IO.FileAttributes.Encrypted] = FileAttributes.Encrypted,
                [IO.FileAttributes.Hidden] = FileAttributes.Hidden,
                [IO.FileAttributes.Offline] = FileAttributes.Offline,
                [IO.FileAttributes.ReadOnly] = FileAttributes.ReadOnly,
                [IO.FileAttributes.System] = FileAttributes.System,
                [IO.FileAttributes.Temporary] = FileAttributes.Temporary,
            }.ToImmutableDictionary();

        public static FileAttributes ToFileAttribute(this IO.FileAttributes attribute)
            => attributes
                .Where(fileAttribute => attribute.HasFlag(fileAttribute.Key))
                .Select(fileAttribute => fileAttribute.Value)
                .Aggregate((result, attribute) => result | attribute);
    }
}
