using System;

namespace Files.Backend.Item
{
    public interface IRecycleBinItem : IFileItem
    {
        DateTime ItemDateDeleted { get; }
    }
}
